using System.ComponentModel.DataAnnotations;

namespace blog.Domain.Common.Settings
{
    public class AccountLockoutSettings
    {
        [Range(1, int.MaxValue)]
        public int MaxFailedAttempts { get; init; }

        [Range(1, int.MaxValue)]
        public int LockoutDurationMinutes { get; init; }
    }
}
