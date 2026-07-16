namespace blog.Application.Users.Commands.Register
{
    public class RegisterResponse
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public int ExpiryMinutes { get; init; }
    }
}
