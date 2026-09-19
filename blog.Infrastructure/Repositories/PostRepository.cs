using blog.Domain.Categories.Common;
using blog.Domain.Categories.Types;
using blog.Domain.Common;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Entities;
using blog.Domain.Posts.Enums;
using blog.Domain.Posts.Repository;
using blog.Domain.Posts.Types;
using blog.Domain.Users.Common;
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
                .Include(x => x.Author)
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.Slug == slug, ct);

        public async Task<PagedResult<Post>> GetAllAsync(PagedRequest paging, PostSortBy sortBy = PostSortBy.Newest, PostFilter filter = PostFilter.All, bool includeDrafts = true, CancellationToken ct = default)
        {
            var query = context.Posts
                .Include(x => x.Author)
                .Include(x => x.Category)
                .AsQueryable();

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

            query = query.ApplySorting(sortBy);

            return await query.ToPagedResultAsync(paging, ct);
        }

        public async Task<PagedResult<Post>> GetReportAsync(PagedRequest paging, DateOnly from, DateOnly to, PostFilter filter, PostSortBy sortBy, CategoryId? categoryId, UserId? authorId, CancellationToken ct = default)
        {
            var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var toExclusiveUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            var query = context.Posts
                .Include(x => x.Author)
                .Include(x => x.Category)
                .Where(x => x.CreatedAt >= fromUtc && x.CreatedAt < toExclusiveUtc);

            query = filter switch
            {
                PostFilter.Draft => query.Where(x => x.Status == PostStatus.Draft),
                PostFilter.PendingApproval => query.Where(x => x.Status == PostStatus.PendingApproval),
                PostFilter.Published => query.Where(x => x.Status == PostStatus.Published),
                PostFilter.Rejected => query.Where(x => x.Status == PostStatus.Rejected),
                _ => query
            };

            if (categoryId is not null)
                query = query.Where(x => x.CategoryId == categoryId);

            if (authorId is not null)
                query = query.Where(x => x.AuthorId == authorId);

            query = query.ApplySorting(sortBy);

            return await query.ToPagedResultAsync(paging, ct);
        }

        public async Task<PagedResult<Post>> GetAllPublishedAsync(PagedRequest paging, PostSortBy sortBy = PostSortBy.Newest, CancellationToken ct = default)
        {
            var query = context.Posts
                .Include(x => x.Author)
                .Include(x => x.Category)
                .Where(x => x.Status == PostStatus.Published)
                .ApplySorting(sortBy);

            return await query.ToPagedResultAsync(paging, ct);
        }

        public async Task<PagedResult<Post>> GetByAuthorAsync(PagedRequest paging, UserId authorId, PostSortBy sortBy = PostSortBy.Newest, bool publishedOnly = true, CancellationToken ct = default)
        {
            var query = context.Posts
                .Include(x => x.Author)
                .Include(x => x.Category)
                .Where(x => x.AuthorId == authorId);

            if (publishedOnly)
                query = query.Where(x => x.Status == PostStatus.Published);

            query = query.ApplySorting(sortBy);

            return await query.ToPagedResultAsync(paging, ct);
        }

        public async Task<PagedResult<Post>> GetPendingApprovalAsync(PagedRequest paging, PostSortBy sortBy = PostSortBy.Newest, CancellationToken ct = default)
        {
            var query = context.Posts
                .Include(x => x.Author)
                .Include(x => x.Category)
                .Where(x => x.Status == PostStatus.PendingApproval)
                .ApplySorting(sortBy);

            return await query.ToPagedResultAsync(paging, ct);
        }

        public async Task<PagedResult<Post>> GetByTagAsync(PagedRequest paging, List<string> tags, PostSortBy sortBy = PostSortBy.Newest, PostTagGroupingMode groupingMode = PostTagGroupingMode.None, CancellationToken ct = default)
        {
            var query = context.Posts
                .Include(x => x.Author)
                .Include(x => x.Category)
                .Where(x => x.Status == PostStatus.Published && tags.Any(t => x.Tags.Contains(t)))
                .ApplySorting(sortBy);

            if (groupingMode == PostTagGroupingMode.None)
                return await query.ToPagedResultAsync(paging, ct);

            // Grouping/interleaving needs, per post, the index of the first requested tag it matches.
            // That rank isn't practical to compute in SQL for an arbitrary-length tag list, so the
            // DB does the filtering + sorting (using the existing GIN index), and the relatively small,
            // already-narrowed result set is grouped/interleaved here in memory.
            var matchingPosts = await query.ToListAsync(ct);

            var ranked = matchingPosts
                .Select(post => (Post: post, TagIndex: GetFirstMatchingTagIndex(post.Tags, tags)))
                .ToList();

            var ordered = groupingMode == PostTagGroupingMode.Grouped
                ? ranked.OrderBy(x => x.TagIndex).Select(x => x.Post).ToList()
                : InterleaveByTag(ranked, tags.Count);

            var totalCount = ordered.Count;
            var items = ordered
                .Skip((paging.Page - 1) * paging.PageSize)
                .Take(paging.PageSize)
                .ToList();

            return new PagedResult<Post>(items, totalCount, paging.Page, paging.PageSize);
        }

        public async Task<PagedResult<Post>> GetByCategorySlugAsync(PagedRequest paging, string categorySlug, PostSortBy sortBy = PostSortBy.Newest, CancellationToken ct = default)
        {
            var query = context.Posts
                .Include(x => x.Author)
                .Include(x => x.Category)
                .Where(x => x.Status == PostStatus.Published && x.Category.Slug == categorySlug)
                .ApplySorting(sortBy);

            return await query.ToPagedResultAsync(paging, ct);
        }

        public async Task<PagedResult<Post>> SearchAsync(PagedRequest paging, string term, PostSortBy sortBy = PostSortBy.Newest, CancellationToken ct = default)
        {
            var query = context.Posts
                .Include(x => x.Author)
                .Include(x => x.Category)
                .Where(x => x.Status == PostStatus.Published &&
                            x.SearchVector.Matches(EF.Functions.PlainToTsQuery("english", term)))
                .ApplySorting(sortBy);

            return await query.ToPagedResultAsync(paging, ct);
        }

        public async Task<PostStats> GetStatsAsync(int postsPerDayCount, CancellationToken ct = default)
            => await BuildStatsAsync(context.Posts, postsPerDayCount, ct);

        public async Task<PostStatusReport> GetStatusReportAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
            => await BuildStatusReportAsync(context.Posts, from, to, ct);

        public async Task<PostStatusReport> GetStatusReportByAuthorAsync(UserId authorId, DateOnly from, DateOnly to, CancellationToken ct = default)
            => await BuildStatusReportAsync(context.Posts.Where(x => x.AuthorId == authorId), from, to, ct);

        public async Task<PostStatusReport> GetStatusReportByCategoryAsync(CategoryId categoryId, DateOnly from, DateOnly to, CancellationToken ct = default)
            => await BuildStatusReportAsync(context.Posts.Where(x => x.CategoryId == categoryId), from, to, ct);

        public async Task<IReadOnlyList<CategoryPerformanceResult>> GetCategoryBreakdownAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
        {
            var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var toExclusiveUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            var postsInRange = context.Posts.Where(x => x.CreatedAt >= fromUtc && x.CreatedAt < toExclusiveUtc);

            var query = context.Categories
                .Where(c => !c.IsDeleted)
                .GroupJoin(
                    postsInRange,
                    category => category.Id,
                    post => post.CategoryId,
                    (category, posts) => new CategoryPerformanceResult
                    {
                        CategoryId = category.Id.Value,
                        Name = category.Name,
                        DraftCount = posts.Count(p => p.Status == PostStatus.Draft),
                        PendingApprovalCount = posts.Count(p => p.Status == PostStatus.PendingApproval),
                        PublishedCount = posts.Count(p => p.Status == PostStatus.Published),
                        RejectedCount = posts.Count(p => p.Status == PostStatus.Rejected),
                        TotalCount = posts.Count(),
                        TotalViewCount = posts.Sum(p => (int?)p.ViewCount) ?? 0
                    })
                .OrderBy(x => x.Name);

            return await query.ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Post>> GetTopViewedAsync(DateOnly from, DateOnly to, int topN, CategoryId? categoryId, CancellationToken ct = default)
        {
            var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var toExclusiveUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            var query = context.Posts
                .Include(x => x.Author)
                .Include(x => x.Category)
                .Where(x => x.Status == PostStatus.Published && x.CreatedAt >= fromUtc && x.CreatedAt < toExclusiveUtc);

            if (categoryId is not null)
                query = query.Where(x => x.CategoryId == categoryId);

            return await query
                .OrderByDescending(x => x.ViewCount)
                .Take(topN)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<TopAuthorResult>> GetTopAuthorsAsync(DateOnly from, DateOnly to, int topN, CancellationToken ct = default)
        {
            var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var toExclusiveUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            var query = context.Posts
                .Where(x => x.Status == PostStatus.Published && x.CreatedAt >= fromUtc && x.CreatedAt < toExclusiveUtc)
                .GroupBy(x => new { x.AuthorId, x.Author.FirstName, x.Author.LastName })
                .Select(g => new TopAuthorResult
                {
                    AuthorId = g.Key.AuthorId.Value,
                    FullName = g.Key.FirstName + " " + g.Key.LastName,
                    PublishedCount = g.Count(),
                    TotalViewCount = g.Sum(p => p.ViewCount)
                })
                .OrderByDescending(x => x.TotalViewCount)
                .Take(topN);

            return await query.ToListAsync(ct);
        }

        public async Task<PostStats> GetStatsByAuthorAsync(UserId authorId, int postsPerDayCount, CancellationToken ct = default)
            => await BuildStatsAsync(context.Posts.Where(x => x.AuthorId == authorId), postsPerDayCount, ct);

        public async Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct = default)
            => await context.Posts.AnyAsync(x => x.Slug == slug, ct);

        public async Task<int> CountDraftsByAuthorAsync(UserId authorId, CancellationToken ct = default)
            => await context.Posts.CountAsync(x => x.AuthorId == authorId && x.Status == PostStatus.Draft, ct);

        public async Task AddAsync(Post post, CancellationToken ct = default)
            => await context.Posts.AddAsync(post, ct);

        public void Update(Post post)
            => context.Posts.Update(post);

        public void Delete(Post post)
            => context.Posts.Remove(post);

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

        // Round-robins posts across tag groups (one post from tag[0], then tag[1], ..., then back
        // to tag[0]), preserving each group's existing SortBy order and skipping exhausted groups.
        private static List<Post> InterleaveByTag(List<(Post Post, int TagIndex)> ranked, int tagCount)
        {
            var groups = Enumerable.Range(0, tagCount)
                .Select(i => new Queue<Post>(ranked.Where(x => x.TagIndex == i).Select(x => x.Post)))
                .ToList();

            var result = new List<Post>(ranked.Count);
            var remaining = ranked.Count;

            while (remaining > 0)
            {
                foreach (var group in groups)
                {
                    if (group.Count == 0)
                        continue;

                    result.Add(group.Dequeue());
                    remaining--;
                }
            }

            return result;
        }

        private static async Task<PostStats> BuildStatsAsync(IQueryable<Post> query, int postsPerDayCount, CancellationToken ct)
        {
            var counts = await query
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Draft = g.Count(x => x.Status == PostStatus.Draft),
                    PendingApproval = g.Count(x => x.Status == PostStatus.PendingApproval),
                    Published = g.Count(x => x.Status == PostStatus.Published),
                    Rejected = g.Count(x => x.Status == PostStatus.Rejected),
                    TotalViews = g.Sum(x => x.ViewCount)
                })
                .FirstOrDefaultAsync(ct);

            var since = DateTime.UtcNow.Date.AddDays(-(postsPerDayCount - 1));

            var rows = await query
                .Where(p => p.CreatedAt >= since)
                .GroupBy(p => p.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(e => e.Date)
                .ToListAsync(ct);

            var postsPerDay = rows
                .Select(r => new DailyCount(DateOnly.FromDateTime(r.Date), r.Count))
                .ToList();


            return new PostStats
            {
                TotalCount = counts?.Total ?? 0,
                DraftCount = counts?.Draft ?? 0,
                PendingApprovalCount = counts?.PendingApproval ?? 0,
                PublishedCount = counts?.Published ?? 0,
                RejectedCount = counts?.Rejected ?? 0,
                TotalViewCount = counts?.TotalViews ?? 0,
                PostsPerDay = postsPerDay
            };
        }

        private static async Task<PostStatusReport> BuildStatusReportAsync(IQueryable<Post> query, DateOnly from, DateOnly to, CancellationToken ct)
        {
            var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var toExclusiveUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            var inRange = query.Where(x => x.CreatedAt >= fromUtc && x.CreatedAt < toExclusiveUtc);

            var counts = await inRange
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Draft = g.Count(x => x.Status == PostStatus.Draft),
                    PendingApproval = g.Count(x => x.Status == PostStatus.PendingApproval),
                    Published = g.Count(x => x.Status == PostStatus.Published),
                    Rejected = g.Count(x => x.Status == PostStatus.Rejected)
                })
                .FirstOrDefaultAsync(ct);

            var dailyRows = await inRange
                .GroupBy(x => new { x.CreatedAt.Date, x.Status })
                .Select(g => new { g.Key.Date, g.Key.Status, Count = g.Count() })
                .ToListAsync(ct);

            var dailyBreakdown = dailyRows
                .GroupBy(r => r.Date)
                .Select(g => new PostStatusDailyCount(
                    DateOnly.FromDateTime(g.Key),
                    g.Where(x => x.Status == PostStatus.Draft).Sum(x => x.Count),
                    g.Where(x => x.Status == PostStatus.PendingApproval).Sum(x => x.Count),
                    g.Where(x => x.Status == PostStatus.Published).Sum(x => x.Count),
                    g.Where(x => x.Status == PostStatus.Rejected).Sum(x => x.Count)))
                .OrderBy(x => x.Date)
                .ToList();

            return new PostStatusReport
            {
                From = from,
                To = to,
                TotalCount = counts?.Total ?? 0,
                DraftCount = counts?.Draft ?? 0,
                PendingApprovalCount = counts?.PendingApproval ?? 0,
                PublishedCount = counts?.Published ?? 0,
                RejectedCount = counts?.Rejected ?? 0,
                DailyBreakdown = dailyBreakdown
            };
        }
    }
}