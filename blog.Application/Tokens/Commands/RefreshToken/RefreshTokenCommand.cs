using MediatR;

namespace blog.Application.Tokens.Commands.RefreshToken
{
    public class RefreshTokenCommand : IRequest<RefreshTokenResponse>
    {
        public string RefreshToken { get; init; } = string.Empty;
        public string DeviceInfo { get; init; } = string.Empty;
    }
}
