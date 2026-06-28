namespace blog.Api.DTOs.Users
{
    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; init; } = string.Empty;
        public string NewPassword { get; init; } = string.Empty;
    }
}
