namespace blog.Application.Users.Commands.ForgotPassword
{
    public class ForgotPasswordResponse
    {
        public DateTime ExpiresAt { get; init; }
        public bool Success { get; init; }
    }
}
