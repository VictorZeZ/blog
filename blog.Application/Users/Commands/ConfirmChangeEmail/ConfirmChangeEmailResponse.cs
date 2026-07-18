namespace blog.Application.Users.Commands.ConfirmChangeEmail
{
    public class ConfirmChangeEmailResponse
    {
        public bool Success { get; init; }
        public int ExpiryMinutes { get; init; }
    }
}
