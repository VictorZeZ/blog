using blog.Domain.Common.Settings;
using blog.Domain.EmailVerifications.Enums;
using blog.Domain.Exceptions;

namespace blog.Domain.EmailVerifications.Extensions
{
    public static class EmailVerificationSettingsExtensions
    {
        public static int GetExpiryMinutes(this EmailVerificationSettings settings, EmailVerificationPurpose purpose) => purpose switch
        {
            EmailVerificationPurpose.Registration => settings.RegistrationExpiryMinutes,
            EmailVerificationPurpose.LoginVerification => settings.LoginVerificationExpiryMinutes,
            EmailVerificationPurpose.ChangeEmail => settings.ChangeEmailExpiryMinutes,
            EmailVerificationPurpose.ResetPassword => settings.ResetPasswordExpiryMinutes,
            EmailVerificationPurpose.ConfirmNewEmail => settings.ConfirmNewEmailExpiryMinutes,
            _ => throw new UnsupportedOperationException($"EmailVerificationPurpose.{purpose}")
        };

        public static int GetMaxAttempts(this EmailVerificationSettings settings, EmailVerificationPurpose purpose) => purpose switch
        {
            EmailVerificationPurpose.Registration => settings.RegistrationMaxAttempts,
            EmailVerificationPurpose.LoginVerification => settings.LoginVerificationMaxAttempts,
            EmailVerificationPurpose.ChangeEmail => settings.ChangeEmailMaxAttempts,
            EmailVerificationPurpose.ResetPassword => settings.ResetPasswordMaxAttempts,
            EmailVerificationPurpose.ConfirmNewEmail => settings.ConfirmNewEmailMaxAttempts,
            _ => throw new UnsupportedOperationException($"EmailVerificationPurpose.{purpose}")
        };
    }
}
