using blog.Domain.Common.Interfaces;
using MediatR;

namespace blog.Application.Users.Commands.ChangeEmail
{
    public class ChangeEmailCommand : IRequest<ChangeEmailResponse>, IRequirePasswordConfirmation
    {
        public Guid UserId { get; init; }
        public string NewEmail { get; init; } = string.Empty;
        public string CurrentPassword { get; init; } = string.Empty;

        public bool RequiresPasswordConfirmation => true;
    }
}