using blog.Domain.EmailVerifications.Entities;
using blog.Domain.EmailVerifications.Enums;
using blog.Domain.EmailVerifications.Repository;
using blog.Domain.EmailVerifications.Types;
using blog.Domain.Users.Types;
using blog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace blog.Infrastructure.Repositories
{
    public class EmailVerificationRepository(AppDbContext context) : IEmailVerificationRepository
    {
        public async Task<EmailVerification?> GetActiveByUserIdAsync(UserId userId, CancellationToken ct = default)
            => await context.EmailVerifications
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Status == EmailVerificationStatus.Active, ct);
        public async Task<EmailVerification?> GetByIdAsync(EmailVerificationId verificationId, CancellationToken ct = default)
            => await context.EmailVerifications
                .FirstOrDefaultAsync(x => x.Id == verificationId, ct);

        public async Task AddAsync(EmailVerification emailVerification, CancellationToken ct = default)
            => await context.EmailVerifications.AddAsync(emailVerification, ct);

        public void Update(EmailVerification emailVerification)
            => context.EmailVerifications.Update(emailVerification);

        public async Task DeleteExpiredAsync(CancellationToken ct = default)
            => await context.EmailVerifications
                .Where(x => x.Status == EmailVerificationStatus.Expired || x.ExpiresAt < DateTime.UtcNow)
                .ExecuteDeleteAsync(ct);
    }
}
