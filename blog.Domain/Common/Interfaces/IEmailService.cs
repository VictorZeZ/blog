namespace blog.Domain.Common.Interfaces
{
    public interface IEmailService
    {
        Task<string> SendVerificationCodeAsync(string recipientEmail, int expiryMinutes, CancellationToken ct = default);
    }
}