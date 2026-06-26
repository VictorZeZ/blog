using MediatR;

namespace blog.Application.Users.Commands.UpdateUser
{
    public class UpdateUserCommand : IRequest<UpdateUserResponse>
    {
        public Guid UserId { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
    }
}
