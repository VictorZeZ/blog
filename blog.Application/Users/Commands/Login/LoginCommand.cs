using MediatR;

namespace blog.Application.Users.Commands.Login
{
    public class LoginCommand : IRequest<LoginResponse>
    {
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string DeviceInfo { get; init; } = string.Empty;
    }
}
