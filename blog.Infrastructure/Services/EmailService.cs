using blog.Domain.Common.Interfaces;
using blog.Domain.Common.Settings;
using blog.Domain.EmailVerifications.Enums;
using blog.Domain.EmailVerifications.Extensions;
using Microsoft.Extensions.Options;

namespace blog.Infrastructure.Services
{
    public class EmailService(IEmailSender emailSender, IEmailTemplateRenderer templateRenderer, IVerificationCodeGenerator codeGenerator, IHasher hasher, IOptions<EmailVerificationSettings> emailVerificationSettings) : IEmailService
    {
        public async Task<string> SendVerificationCodeAsync(string recipientEmail, EmailVerificationPurpose purpose, CancellationToken ct = default)
        {
            var expiryMinutes = emailVerificationSettings.Value.GetExpiryMinutes(purpose);

            var verificationCode = codeGenerator.Generate();
            var codeHash = hasher.Hash(verificationCode.Code);

            var (title, description) = purpose switch
            {
                EmailVerificationPurpose.Registration => (
                    "Verify your email address",
                    "Please enter the One-Time Password (OTP) below to verify your email address and activate your account."
                ),

                EmailVerificationPurpose.LoginVerification => (
                    "Verify your sign in",
                    "Please enter the One-Time Password (OTP) below to complete your sign in."
                ),

                EmailVerificationPurpose.ChangeEmail => (
                    "Verify your identity",
                    "Please enter the One-Time Password (OTP) below to verify your identity before changing your email address."
                ),

                EmailVerificationPurpose.ConfirmNewEmail => (
                    "Confirm your new email address",
                    "Please enter the One-Time Password (OTP) below to confirm your new email address."
                ),

                EmailVerificationPurpose.ResetPassword => (
                    "Reset your password",
                    "Please enter the One-Time Password (OTP) below to continue resetting your password."
                ),

                _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, null)
            };

            var htmlBody = templateRenderer.RenderVerificationCode(
                title,
                description,
                verificationCode.DisplayCode,
                expiryMinutes);

            await emailSender.SendAsync(
                recipientEmail,
                title,
                htmlBody,
                ct);

            return codeHash;
        }
    }
}
