namespace blog.Application.Users.Commands.ChangeEmail
{
    public class ChangeEmailResponse
    {
        public bool Success { get; init; }
        public int ExpiryMinutes { get; init; }
    }
}
