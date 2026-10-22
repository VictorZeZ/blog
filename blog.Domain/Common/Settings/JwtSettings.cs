using System.ComponentModel.DataAnnotations;

namespace blog.Domain.Common.Settings
{
    public class JwtSettings
    {
        [Required, MinLength(32)]
        public string SecretKey { get; init; } = string.Empty;

        [Required]
        public string Issuer { get; init; } = string.Empty;

        [Required]
        public string Audience { get; init; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int AccessTokenExpiryMinutes { get; init; }

        [Range(1, int.MaxValue)]
        public int RefreshTokenExpiryDays { get; init; }
    }
}