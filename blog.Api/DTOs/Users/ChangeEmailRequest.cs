namespace blog.Api.DTOs.Users
{
    public class ChangeEmailRequest
    {
        public string NewEmail { get; init; } = string.Empty;
        public string CurrentPassword { get; init; } = string.Empty;
    }
}
