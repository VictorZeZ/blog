using MediatR;

namespace blog.Application.Users.Commands.ChangeEmail
{
    public class ChangeEmailCommand : IRequest<ChangeEmailResponse>
    {
        public Guid UserId { get; init; }
        public string NewEmail { get; init; } = string.Empty;
        public string CurrentPassword { get; init; } = string.Empty;
    }
}
