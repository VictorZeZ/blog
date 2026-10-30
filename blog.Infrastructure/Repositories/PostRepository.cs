using blog.Domain.Categories.Types;
using blog.Domain.Common;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Entities;
using blog.Domain.Posts.Enums;
using blog.Domain.Posts.Repository;
using blog.Domain.Posts.Types;
using blog.Domain.Users.Types;
using blog.Infrastructure.Persistence;
using blog.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace blog.Infrastructure.Repositories
{
    public class PostRepository(AppDbContext context) : IPostRepository
    {
        public async Task<Post?> GetByIdAsync(PostId id, CancellationToken ct = default)
            => await context.Posts
                .Include(x => x.Author)
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

        public async Task<Post?> GetBySlugAsync(string slug, CancellationToken ct = default)
            => await context.Posts
                .AsNoTracking()
                .Include(x => x.Author)
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.Slug == slug, ct);

        public async Task<PagedResult<PostSummaryResponse>> GetAllAsync(PagedRequest paging, PostSortBy sortBy = PostSortBy.Newest, PostFilter filter = PostFilter.All, bool includeDrafts = true, CancellationToken ct = default)
        {
            var query = context.Posts.AsQueryable();

            query = filter switch
            {
                PostFilter.Draft => query.Where(x => x.Status == PostStatus.Draft),
                PostFilter.PendingApproval => query.Where(x => x.Status == PostStatus.PendingApproval),
                PostFilter.Published => query.Where(x => x.Status == PostStatus.Published),
                PostFilter.Rejected => query.Where(x => x.Status == PostStatus.Rejected),
                _ => query
            };

            if (!includeDrafts)
                query = query.Where(x => x.Status != PostStatus.Draft);

            return await query.ToSummaryPagedResultAsync(paging, sortBy, ct);
        }

        public async Task<PagedResult<PostSummaryResponse>> GetAllPublishedAsync(PagedRequest paging, PostSortBy sortBy = PostSortBy.Newest, CancellationToken ct = default)
            => await context.Posts
                .Where(x => x.Status == PostStatus.Published)
                .ToSummaryPagedResultAsync(paging, sortBy, ct);

        public async Task<PagedResult<PostSummaryResponse>> GetByAuthorAsync(PagedRequest paging, UserId authorId, PostSortBy sortBy = PostSortBy.Newest, bool publishedOnly = true, bool includeDrafts = false, CancellationToken ct = default)
        {
            var query = context.Posts
                .Where(x => x.AuthorId == authorId);

            if (publishedOnly)
                query = query.Where(x => x.Status == PostStatus.Published);
            else if (!includeDrafts)
                query = query.Where(x => x.Status != PostStatus.Draft);

            return await query.ToSummaryPagedResultAsync(paging, sortBy, ct);
        }

        public async Task<PagedResult<PostSummaryResponse>> GetPendingApprovalAsync(PagedRequest paging, PostSortBy sortBy = PostSortBy.Newest, CancellationToken ct = default)
            => await context.Posts
                .Where(x => x.Status == PostStatus.PendingApproval)
                .ToSummaryPagedResultAsync(paging, sortBy, ct);

        public async Task<PagedResult<PostSummaryResponse>> GetByTagAsync(PagedRequest paging, List<string> tags, PostSortBy sortBy = PostSortBy.Newest, PostTagGroupingMode groupingMode = PostTagGroupingMode.None, CancellationToken ct = default)
        {
            var query = context.Posts
                .Where(x => x.Status == PostStatus.Published && tags.Any(t => x.Tags.Contains(t)));

            if (groupingMode == PostTagGroupingMode.None)
                return await query.ToSummaryPagedResultAsync(paging, sortBy, ct);

            // Grouping/interleaving needs, per post, the index of the first requested tag it matches.
            // That rank isn't practical to compute in SQL for an arbitrary-length tag list, so the DB
            // does the filtering + sorting and returns only (Id, Tags) per match; the lightweight ids are
            // ranked in memory and just the requested page is then loaded as summaries.
            var matches = await query
                .ApplySorting(sortBy)
                .Select(x => new { x.Id, x.Tags })
                .ToListAsync(ct);

            var ranked = matches
                .Select(match => (match.Id, TagIndex: GetFirstMatchingTagIndex(match.Tags, tags)))
                .ToList();

            var orderedIds = groupingMode == PostTagGroupingMode.Grouped
                ? ranked.OrderBy(x => x.TagIndex).Select(x => x.Id).ToList()
                : InterleaveByTag(ranked, tags.Count);

            var pageIds = orderedIds
                .Skip((paging.Page - 1) * paging.PageSize)
                .Take(paging.PageSize)
                .ToList();

            var summaries = await context.Posts
                .Where(x => pageIds.Contains(x.Id))
                .SelectSummary()
                .ToListAsync(ct);

            var items = pageIds
                .Join(summaries, id => id.Value, summary => summary.Id, (_, summary) => summary)
                .ToList();

            return new PagedResult<PostSummaryResponse>(items, orderedIds.Count, paging.Page, paging.PageSize);
        }

        public async Task<PagedResult<PostSummaryResponse>> GetByCategorySlugAsync(PagedRequest paging, string categorySlug, PostSortBy sortBy = PostSortBy.Newest, CancellationToken ct = default)
            => await context.Posts
                .Where(x => x.Status == PostStatus.Published && x.Category.Slug == categorySlug)
                .ToSummaryPagedResultAsync(paging, sortBy, ct);

        public async Task<PagedResult<PostSummaryResponse>> SearchAsync(PagedRequest paging, string term, PostSortBy sortBy = PostSortBy.Newest, CancellationToken ct = default)
            => await context.Posts
                .Where(x => x.Status == PostStatus.Published &&
                            x.SearchVector.Matches(EF.Functions.PlainToTsQuery("english", term)))
                .ToSummaryPagedResultAsync(paging, sortBy, ct);

        public async Task<IReadOnlyList<PostStatusDailyCount>> GetDailyStatusReportAsync(DateOnly from, DateOnly to, CategoryId? categoryId, UserId? authorId, CancellationToken ct = default)
        {
            var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var toExclusiveUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            var query = context.Posts
                .Where(x => x.CreatedAt >= fromUtc && x.CreatedAt < toExclusiveUtc);

            if (categoryId is not null)
                query = query.Where(x => x.CategoryId == categoryId);

            if (authorId is not null)
                query = query.Where(x => x.AuthorId == authorId);

            var grouped = await query
                .GroupBy(x => new { Date = x.CreatedAt.Date, x.Status })
                .Select(g => new { g.Key.Date, g.Key.Status, Count = g.Count() })
                .ToListAsync(ct);

            var byDate = grouped
                .GroupBy(x => DateOnly.FromDateTime(x.Date))
                .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.Status, x => x.Count));

            var days = new List<PostStatusDailyCount>();
            for (var date = from; date <= to; date = date.AddDays(1))
            {
                var counts = byDate.GetValueOrDefault(date);

                days.Add(new PostStatusDailyCount(
                    date,
                    counts?.GetValueOrDefault(PostStatus.Draft) ?? 0,
                    counts?.GetValueOrDefault(PostStatus.PendingApproval) ?? 0,
                    counts?.GetValueOrDefault(PostStatus.Published) ?? 0,
                    counts?.GetValueOrDefault(PostStatus.Rejected) ?? 0));
            }

            return days;
        }

        public async Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct = default)
            => await context.Posts.AnyAsync(x => x.Slug == slug, ct);

        public async Task<int> CountDraftsByAuthorAsync(UserId authorId, CancellationToken ct = default)
            => await context.Posts.CountAsync(x => x.AuthorId == authorId && x.Status == PostStatus.Draft, ct);

        public async Task<PostOverviewCounts> GetOverviewCountsAsync(UserId? authorId, CancellationToken ct = default)
        {
            var query = context.Posts.AsQueryable();

            if (authorId is not null)
                query = query.Where(x => x.AuthorId == authorId);

            var counts = await query
                .GroupBy(_ => 1)
                .Select(g => new PostOverviewCounts(
                    g.Count(x => x.Status == PostStatus.Draft),
                    g.Count(x => x.Status == PostStatus.PendingApproval),
                    g.Count(x => x.Status == PostStatus.Published),
                    g.Count(x => x.Status == PostStatus.Rejected),
                    g.Sum(x => x.Status == PostStatus.Published ? x.ViewCount : 0)))
                .FirstOrDefaultAsync(ct);

            return counts ?? new PostOverviewCounts(0, 0, 0, 0, 0);
        }

        public async Task AddAsync(Post post, CancellationToken ct = default)
            => await context.Posts.AddAsync(post, ct);

        public void Update(Post post)
            => context.Posts.Update(post);

        public void Delete(Post post)
            => context.Posts.Remove(post);

        public async Task IncrementViewCountAsync(PostId id, CancellationToken ct = default)
            => await context.Posts
                .Where(x => x.Id == id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ViewCount, x => x.ViewCount + 1), ct);

        // Returns the index (in the caller-supplied tag list) of the first tag a post matches,
        // so a post matching multiple requested tags is grouped/interleaved under only one of them
        // — the earliest one in the input order — and never appears twice in the result.
        private static int GetFirstMatchingTagIndex(List<string> postTags, List<string> requestedTags)
        {
            for (var i = 0; i < requestedTags.Count; i++)
            {
                if (postTags.Contains(requestedTags[i]))
                    return i;
            }

            // Unreachable given the DB-level filter already guarantees at least one match,
            // but kept as a safe fallback rather than throwing.
            return requestedTags.Count;
        }

        // Round-robins post ids across tag groups (one from tag[0], then tag[1], ..., then back
        // to tag[0]), preserving each group's existing SortBy order and skipping exhausted groups.
        private static List<PostId> InterleaveByTag(List<(PostId Id, int TagIndex)> ranked, int tagCount)
        {
            var groups = Enumerable.Range(0, tagCount)
                .Select(i => new Queue<PostId>(ranked.Where(x => x.TagIndex == i).Select(x => x.Id)))
                .ToList();

            var result = new List<PostId>(ranked.Count);

            while (groups.Exists(group => group.Count > 0))
            {
                foreach (var group in groups)
                {
                    if (group.Count == 0)
                        continue;

                    result.Add(group.Dequeue());
                }
            }

            return result;
        }
    }
}