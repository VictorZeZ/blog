using System.ComponentModel.DataAnnotations;

namespace blog.Domain.Common.Settings
{
    public class EmailVerificationSettings
    {
        [Range(1, int.MaxValue)]
        public int RegistrationExpiryMinutes { get; init; }

        [Range(1, int.MaxValue)]
        public int RegistrationMaxAttempts { get; init; }

        [Range(1, int.MaxValue)]
        public int LoginVerificationExpiryMinutes { get; init; }

        [Range(1, int.MaxValue)]
        public int LoginVerificationMaxAttempts { get; init; }

        [Range(1, int.MaxValue)]
        public int ChangeEmailExpiryMinutes { get; init; }

        [Range(1, int.MaxValue)]
        public int ChangeEmailMaxAttempts { get; init; }
    }
}
