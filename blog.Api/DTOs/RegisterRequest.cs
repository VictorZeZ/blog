namespace blog.Api.DTOs
{
    public class RegisterRequest
    {
        public string Email { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
    }
}
