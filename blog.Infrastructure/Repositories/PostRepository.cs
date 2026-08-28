using blog.Domain.Common;
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
                .Include(x => x.Author)
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.Slug == slug, ct);

        public async Task<PagedResult<Post>> GetAllAsync(PagedRequest paging, PostSortBy sortBy = PostSortBy.Newest, PostFilter filter = PostFilter.All, CancellationToken ct = default)
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
    }
}