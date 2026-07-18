namespace blog.Application.Users.Commands.ConfirmNewEmail
{
    public class ConfirmNewEmailResponse
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = string.Empty;
    }
}
