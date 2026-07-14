using blog.Domain.EmailVerifications.Entities;
using blog.Domain.Users.Types;

namespace blog.Domain.EmailVerifications.Repository
{
    public interface IEmailVerificationRepository
    {
        // Read
        Task<EmailVerification?> GetActiveByUserIdAsync(UserId userId, CancellationToken ct = default);

        // Write
        Task AddAsync(EmailVerification emailVerification, CancellationToken ct = default);
        void Update(EmailVerification emailVerification);
        Task DeleteExpiredAsync(CancellationToken ct = default);
    }
}
