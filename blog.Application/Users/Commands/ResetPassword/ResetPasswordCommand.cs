using MediatR;

namespace blog.Application.Users.Commands.ResetPassword
{
    public class ResetPasswordCommand : IRequest<ResetPasswordResponse>
    {
        public string Email { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
        public string NewPassword { get; init; } = string.Empty;
    }
}
