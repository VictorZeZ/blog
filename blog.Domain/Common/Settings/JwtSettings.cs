namespace blog.Domain.Common.Settings
{
    public class JwtSettings
    {
        public string SecretKey { get; init; } = string.Empty;
        public string Issuer { get; init; } = string.Empty;
        public string Audience { get; init; } = string.Empty;
        public int AccessTokenExpiryMinutes { get; init; }
        public int RefreshTokenExpiryDays { get; init; }
    }
}
