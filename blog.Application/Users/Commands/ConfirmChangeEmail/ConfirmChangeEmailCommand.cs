using MediatR;

namespace blog.Application.Users.Commands.ConfirmChangeEmail
{
    public class ConfirmChangeEmailCommand : IRequest<ConfirmChangeEmailResponse>
    {
        public Guid UserId { get; init; }
        public string Code { get; init; } = string.Empty;
    }
}
