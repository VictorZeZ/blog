namespace blog.Api.DTOs.Users
{
    public class ConfirmLoginRequest
    {
        public Guid ChallengeId { get; init; }
        public string Code { get; init; } = string.Empty;
    }
}
