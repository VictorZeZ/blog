using MediatR;

namespace blog.Application.Users.Commands.DeleteAccount
{
    public class DeleteAccountCommand : IRequest<DeleteAccountResponse>
    {
        public Guid UserId { get; init; }
    }
}
