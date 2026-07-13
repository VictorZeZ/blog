using blog.Domain.Common.Interfaces;

namespace blog.Infrastructure.Services
{
    public class EmailService(IEmailSender emailSender, IEmailTemplateRenderer templateRenderer, IVerificationCodeGenerator codeGenerator, IHasher hasher) : IEmailService
    {
        public async Task<string> SendVerificationCodeAsync(string recipientEmail, int expiryMinutes, CancellationToken ct = default)
        {
            var verificationCode = codeGenerator.Generate();
            var codeHash = hasher.Hash(verificationCode.Code);

            var htmlBody = templateRenderer.RenderVerificationCode(verificationCode.DisplayCode, expiryMinutes);
            await emailSender.SendAsync(recipientEmail, "Verify your identity", htmlBody, ct);

            return codeHash;
        }
    }
}
