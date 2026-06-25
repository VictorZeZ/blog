using blog.Domain.Common;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Enums;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using blog.Infrastructure.Persistence;
using blog.Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace blog.Infrastructure.Repositories
{
    public class UserRepository(AppDbContext context) : IUserRepository
    {
        public async Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default)
            => await context.Users.FirstOrDefaultAsync(x => x.Id == id, ct);

        public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
            => await context.Users.FirstOrDefaultAsync(x => x.Email == email.ToLowerInvariant(), ct);

        public async Task<PagedResult<User>> GetAllAsync(PagedRequest paging, UserSortBy sortBy = UserSortBy.Newest, UserFilter filter = UserFilter.All, CancellationToken ct = default)
        {
            var query = context.Users.AsQueryable();

            query = filter switch
            {
                UserFilter.Normal => query.Where(x => x.Level == UserLevel.Normal),
                UserFilter.Author => query.Where(x => x.Level == UserLevel.Author),
                UserFilter.Admin => query.Where(x => x.Level == UserLevel.Admin),
                UserFilter.Owner => query.Where(x => x.Level == UserLevel.Owner),
                _ => query
            };

            query = sortBy switch
            {
                UserSortBy.Oldest => query.OrderBy(x => x.CreatedAt),
                UserSortBy.HighestLevel => query.OrderByDescending(x => x.Level),
                UserSortBy.LowestLevel => query.OrderBy(x => x.Level),
                _ => query.OrderByDescending(x => x.CreatedAt)
            };

            return await query.ToPagedResultAsync(paging, ct);
        }

        public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
            => await context.Users.AnyAsync(x => x.Email == email.ToLowerInvariant(), ct);

        public async Task AddAsync(User user, CancellationToken ct = default)
            => await context.Users.AddAsync(user, ct);

        public void Update(User user)
            => context.Users.Update(user);

        public void SoftDelete(User user)
        {
            user.SoftDelete();
            context.Users.Update(user);
        }
    }
}
