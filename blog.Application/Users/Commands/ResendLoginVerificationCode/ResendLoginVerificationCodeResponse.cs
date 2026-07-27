namespace blog.Application.Users.Commands.ResendLoginVerificationCode
{
    public class ResendLoginVerificationCodeResponse
    {
        public bool Success { get; init; }
        public Guid ChallengeId { get; init; }
        public DateTime ExpiresAt { get; init; }
    }
}
