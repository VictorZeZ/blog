using MediatR;

namespace blog.Application.Users.Commands.ConfirmNewEmail
{
    public class ConfirmNewEmailCommand : IRequest<ConfirmNewEmailResponse>
    {
        public Guid UserId { get; init; }
        public string Code { get; init; } = string.Empty;
    }
}
