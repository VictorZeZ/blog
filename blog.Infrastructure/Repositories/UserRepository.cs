using blog.Domain.Common;
using blog.Domain.Common.Helpers;
using blog.Domain.Posts.Enums;
using blog.Domain.Users.Common;
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
            => await context.Users.FirstOrDefaultAsync(x => x.Email == EmailNormalizer.Normalize(email), ct);

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
            => await context.Users.AnyAsync(x => x.Email == EmailNormalizer.Normalize(email), ct);

        public async Task<PagedResult<UserSearchResult>> SearchAsync(PagedRequest paging, string term, bool isElevatedActor, CancellationToken ct = default)
        {
            var pattern = $"%{EscapeLikePattern(term)}%";

            var query = context.Users
                .Where(x => !x.IsDeleted && !x.IsBanned);

            if (!isElevatedActor)
                query = query.Where(x => x.Level != UserLevel.Normal);

            query = isElevatedActor
                ? query.Where(x =>
                    EF.Functions.ILike(EF.Property<string>(x, "FullNameSearch"), pattern) ||
                    EF.Functions.ILike(x.Email, pattern))
                : query.Where(x => EF.Functions.ILike(EF.Property<string>(x, "FullNameSearch"), pattern));

            var projected = query
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new UserSearchResult
                {
                    Id = x.Id.Value,
                    FullName = EF.Property<string>(x, "FullNameSearch"),
                    Email = isElevatedActor ? x.Email : null,
                    Level = x.Level,
                    CreatedAt = x.CreatedAt,
                    PublishedPostsCount = x.Posts.Count(p => p.Status == PostStatus.Published),
                    TotalViewCount = x.Posts
                        .Where(p => p.Status == PostStatus.Published)
                        .Sum(p => (int?)p.ViewCount) ?? 0
                });

            return await projected.ToPagedResultAsync(paging, ct);
        }

        public async Task AddAsync(User user, CancellationToken ct = default)
            => await context.Users.AddAsync(user, ct);

        public void Update(User user)
            => context.Users.Update(user);

        public void SoftDelete(User user)
        {
            user.SoftDelete();
            context.Users.Update(user);
        }

        // Escapes LIKE/ILIKE wildcard characters in user-supplied search terms so that
        // literal '%' or '_' in a search term is matched as-is rather than as a pattern.
        private static string EscapeLikePattern(string term)
            => term
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");
    }
}