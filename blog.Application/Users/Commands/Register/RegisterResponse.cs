namespace blog.Application.Users.Commands.Register
{
    public class RegisterResponse
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
    }
}
