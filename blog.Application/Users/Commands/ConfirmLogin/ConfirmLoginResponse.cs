namespace blog.Application.Users.Commands.ConfirmLogin
{
    public class ConfirmLoginResponse
    {
        public string AccessToken { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;
    }
}
