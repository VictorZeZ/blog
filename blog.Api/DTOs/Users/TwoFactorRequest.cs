namespace blog.Api.DTOs.Users
{
    public class TwoFactorRequest
    {
        public bool TwoFactor { get; init; }
        public string CurrentPassword { get; init; } = string.Empty;
    }
}