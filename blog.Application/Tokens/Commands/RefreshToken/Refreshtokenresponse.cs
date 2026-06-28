namespace blog.Application.Tokens.Commands.RefreshToken
{
    public class RefreshTokenResponse
    {
        public string AccessToken { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;
    }
}
