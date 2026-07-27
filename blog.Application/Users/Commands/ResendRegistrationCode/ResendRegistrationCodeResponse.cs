namespace blog.Application.Users.Commands.ResendRegistrationCode
{
    public class ResendRegistrationCodeResponse
    {
        public bool Success { get; init; }
        public DateTime ExpiresAt { get; init; }
    }
}
