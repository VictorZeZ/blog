using blog.Domain.Tokens.Entities;
using blog.Domain.Users.Types;

namespace blog.Domain.Tokens.Repository
{
    public interface IRefreshTokenRepository
    {
        // Read
        Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);
        Task<IEnumerable<RefreshToken>> GetActiveByUserIdAsync(UserId userId, CancellationToken ct = default);

        // Write
        Task AddAsync(RefreshToken token, CancellationToken ct = default);
        void Update(RefreshToken token);
        Task DeleteExpiredAsync(CancellationToken ct = default);
    }
}
