using System.ComponentModel.DataAnnotations;

namespace blog.Domain.Common.Settings
{
    public class JwtSettings
    {
        public string SecretKey { get; init; } = string.Empty;
        public string Issuer { get; init; } = string.Empty;
        public string Audience { get; init; } = string.Empty;
        public int AccessTokenExpiryMinutes { get; init; }

        [Range(1, int.MaxValue)]
        public int RefreshTokenExpiryDays { get; init; }
    }
}