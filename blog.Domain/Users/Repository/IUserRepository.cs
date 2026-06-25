using blog.Domain.Common;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Enums;
using blog.Domain.Users.Types;

namespace blog.Domain.Users.Repository
{
    public interface IUserRepository
    {
        // Read
        Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
        Task<PagedResult<User>> GetAllAsync(PagedRequest paging, UserSortBy sortBy = UserSortBy.Newest, UserFilter filter = UserFilter.All, CancellationToken ct = default);
        Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);

        // Write
        Task AddAsync(User user, CancellationToken ct = default);
        void Update(User user);
        void Delete(User user);
    }
}
