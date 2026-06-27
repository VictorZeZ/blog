using MediatR;

namespace blog.Application.Users.Queries.GetUserById
{
    public class GetUserByIdQuery : IRequest<GetUserByIdResponse>
    {
        public Guid UserId { get; init; }
    }
}
