namespace blog.Application.Users.Commands.Login
{
    public class LoginResponse
    {
        public bool RequiresTwoFactor { get; init; }
        public Guid? ChallengeId { get; init; }
        public string? AccessToken { get; init; }
        public string? RefreshToken { get; init; }
    }
}
