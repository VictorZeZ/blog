namespace blog.Application.Users.Commands.ResendResetPasswordCode
{
    public class ResendResetPasswordCodeResponse
    {
        public bool Success { get; init; }
        public DateTime ExpiresAt { get; init; }
    }
}
