namespace blog.Api.DTOs.Users
{
    public class ConfirmEmailRequest
    {
        public string Email { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
    }
}
