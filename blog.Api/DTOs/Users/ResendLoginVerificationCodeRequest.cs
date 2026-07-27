namespace blog.Api.DTOs.Users
{
    public class ResendLoginVerificationCodeRequest
    {
        public Guid ChallengeId { get; init; }
    }
}
