namespace blog.Api.DTOs.Users
{
    public class ResetPasswordRequest
    {
        public string Email { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
        public string NewPassword { get; init; } = string.Empty;
    }
}
