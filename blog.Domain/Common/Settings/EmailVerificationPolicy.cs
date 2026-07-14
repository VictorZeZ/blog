using System.ComponentModel.DataAnnotations;

namespace blog.Domain.Common.Settings
{
    public class EmailVerificationPolicy
    {
        [Range(1, int.MaxValue)]
        public int ExpiryMinutes { get; init; }

        [Range(1, int.MaxValue)]
        public int MaxAttempts { get; init; }
    }
}
