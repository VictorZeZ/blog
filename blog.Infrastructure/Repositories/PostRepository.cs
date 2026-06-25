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
                .FirstOrDefaultAsync(x => x.Id == id, ct);

        public async Task<Post?> GetBySlugAsync(string slug, CancellationToken ct = default)
            => await context.Posts
                .Include(x => x.Author)
                .FirstOrDefaultAsync(x => x.Slug == slug, ct);

        public async Task<PagedResult<Post>> GetAllPublishedAsync(PagedRequest paging, PostSortBy sortBy = PostSortBy.Newest, CancellationToken ct = default)
        {
            var query = context.Posts
                .Include(x => x.Author)
                .Where(x => x.Status == PostStatus.Published)
                .ApplySorting(sortBy);

            return await query.ToPagedResultAsync(paging, ct);
        }

        public async Task<PagedResult<Post>> GetByAuthorAsync(PagedRequest paging, UserId authorId, PostSortBy sortBy = PostSortBy.Newest, CancellationToken ct = default)
        {
            var query = context.Posts
                .Include(x => x.Author)
                .Where(x => x.AuthorId == authorId)
                .ApplySorting(sortBy);

            return await query.ToPagedResultAsync(paging, ct);
        }

        public async Task<PagedResult<Post>> GetPendingApprovalAsync(PagedRequest paging, PostSortBy sortBy = PostSortBy.Newest, CancellationToken ct = default)
        {
            var query = context.Posts
                .Include(x => x.Author)
                .Where(x => x.Status == PostStatus.PendingApproval)
                .ApplySorting(sortBy);

            return await query.ToPagedResultAsync(paging, ct);
        }

        public async Task<PagedResult<Post>> GetByTagAsync(PagedRequest paging, string tag, PostSortBy sortBy = PostSortBy.Newest, CancellationToken ct = default)
        {
            var query = context.Posts
                .Include(x => x.Author)
                .Where(x => x.Status == PostStatus.Published && EF.Functions.JsonContains(x.Tags, $"[\"{tag}\"]"))
                .ApplySorting(sortBy);

            return await query.ToPagedResultAsync(paging, ct);
        }

        public async Task<PagedResult<Post>> SearchAsync(PagedRequest paging, string term, PostSortBy sortBy = PostSortBy.Newest, CancellationToken ct = default)
        {
            var query = context.Posts
                .Include(x => x.Author)
                .Where(x => x.Status == PostStatus.Published &&
                            EF.Functions.ToTsVector("english", x.Title + " " + x.Content)
                                .Matches(term))
                .ApplySorting(sortBy);

            return await query.ToPagedResultAsync(paging, ct);
        }

        public async Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct = default)
            => await context.Posts.AnyAsync(x => x.Slug == slug, ct);

        public async Task AddAsync(Post post, CancellationToken ct = default)
            => await context.Posts.AddAsync(post, ct);

        public void Update(Post post)
            => context.Posts.Update(post);

        public void Delete(Post post)
            => context.Posts.Remove(post);
    }
}
