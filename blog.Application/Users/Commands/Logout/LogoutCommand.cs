using MediatR;

namespace blog.Application.Users.Commands.Logout
{
    public class LogoutCommand : IRequest<LogoutResponse>
    {
        public string RefreshToken { get; init; } = string.Empty;
    }
}
