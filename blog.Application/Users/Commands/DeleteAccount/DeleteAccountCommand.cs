using blog.Domain.Common.Interfaces;
using MediatR;

namespace blog.Application.Users.Commands.DeleteAccount
{
    public class DeleteAccountCommand : IRequest<DeleteAccountResponse>, IRequirePasswordConfirmation
    {
        public Guid UserId { get; init; }
        public string CurrentPassword { get; init; } = string.Empty;

        public bool RequiresPasswordConfirmation => true;
    }
}