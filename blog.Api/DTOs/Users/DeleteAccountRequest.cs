namespace blog.Api.DTOs.Users
{
    public class DeleteAccountRequest
    {
        public string CurrentPassword { get; init; } = string.Empty;
    }
}
