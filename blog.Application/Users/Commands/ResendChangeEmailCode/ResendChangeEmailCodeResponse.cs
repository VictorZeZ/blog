namespace blog.Application.Users.Commands.ResendChangeEmailCode
{
    public class ResendChangeEmailCodeResponse
    {
        public bool Success { get; init; }
        public DateTime ExpiresAt { get; init; }
    }
}
