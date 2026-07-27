namespace blog.Api.DTOs.Users
{
    public class ResendResetPasswordCodeRequest
    {
        public string Email { get; init; } = string.Empty;
    }
}
