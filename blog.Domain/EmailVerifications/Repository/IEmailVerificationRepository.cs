using blog.Domain.EmailVerifications.Entities;
using blog.Domain.EmailVerifications.Enums;
using blog.Domain.EmailVerifications.Types;
using blog.Domain.Users.Types;

namespace blog.Domain.EmailVerifications.Repository
{
    public interface IEmailVerificationRepository
    {
        // Read
        Task<EmailVerification?> GetActiveByUserIdAsync(UserId userId, IReadOnlyCollection<EmailVerificationPurpose> purposes, CancellationToken ct = default);
        Task<EmailVerification?> GetByIdAsync(EmailVerificationId verificationId, CancellationToken ct = default);

        // Write
        Task AddAsync(EmailVerification emailVerification, CancellationToken ct = default);
        void Update(EmailVerification emailVerification);
        Task DeleteExpiredAsync(CancellationToken ct = default);
    }
}