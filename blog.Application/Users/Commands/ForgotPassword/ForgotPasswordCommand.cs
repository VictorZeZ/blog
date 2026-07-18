using MediatR;

namespace blog.Application.Users.Commands.ForgotPassword
{
    public class ForgotPasswordCommand : IRequest<ForgotPasswordResponse>
    {
        public string Email { get; init; } = string.Empty;
    }
}
