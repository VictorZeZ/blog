using blog.Domain.Tokens.Entities;
using blog.Domain.Tokens.Enums;
using blog.Domain.Tokens.Repository;
using blog.Domain.Users.Types;
using blog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace blog.Infrastructure.Repositories
{
    public class RefreshTokenRepository(AppDbContext context) : IRefreshTokenRepository
    {
        public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
            => await context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == token, ct);

        public async Task<IEnumerable<RefreshToken>> GetActiveByUserIdAsync(UserId userId, CancellationToken ct = default)
            => await context.RefreshTokens.Where(x => x.UserId == userId && x.Status == TokenStatus.Active).ToListAsync(ct);

        public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
            => await context.RefreshTokens.AddAsync(token, ct);

        public void Update(RefreshToken token)
            => context.RefreshTokens.Update(token);

        public async Task DeleteExpiredAsync(CancellationToken ct = default)
            => await context.RefreshTokens.Where(x => x.Status == TokenStatus.Expired || x.ExpiresAt < DateTime.UtcNow).ExecuteDeleteAsync(ct);
    }
}
