using MediatR;

namespace blog.Application.Users.Commands.ResendResetPasswordCode
{
    public class ResendResetPasswordCodeCommand : IRequest<ResendResetPasswordCodeResponse>
    {
        public string Email { get; init; } = string.Empty;
    }
}
