using blog.Domain.EmailVerifications.Enums;

namespace blog.Domain.Common.Interfaces
{
    public interface IEmailService
    {
        Task<string> SendVerificationCodeAsync(string recipientEmail, EmailVerificationPurpose purpose, CancellationToken ct = default);
    }
}