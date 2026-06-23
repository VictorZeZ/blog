using blog.Domain.Common;
using blog.Domain.Posts.Entities;
using blog.Domain.Posts.Enums;
using blog.Domain.Posts.Types;
using blog.Domain.Users.Types;

namespace blog.Domain.Posts.Repository
{
    public interface IPostRepository
    {
        // Read
        Task<Post?> GetByIdAsync(PostId id, CancellationToken ct = default);
        Task<Post?> GetBySlugAsync(string slug, CancellationToken ct = default);
        Task<PagedResult<Post>> GetAllPublishedAsync(PagedRequest paging, PostSortBy sortBy = PostSortBy.Newest, CancellationToken ct = default);
        Task<PagedResult<Post>> GetByAuthorAsync(PagedRequest paging, UserId authorId, PostSortBy sortBy = PostSortBy.Newest, CancellationToken ct = default);
        Task<PagedResult<Post>> GetPendingApprovalAsync(PagedRequest paging, PostSortBy sortBy = PostSortBy.Newest, CancellationToken ct = default);
        Task<PagedResult<Post>> GetByTagAsync(PagedRequest paging, string tag, PostSortBy sortBy = PostSortBy.Newest, CancellationToken ct = default);
        Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct = default);

        // Write
        Task AddAsync(Post post, CancellationToken ct = default);
        void Update(Post post);
        void Delete(Post post);
    }
}
