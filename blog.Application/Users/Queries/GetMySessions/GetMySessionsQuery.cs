using MediatR;

namespace blog.Application.Users.Queries.GetMySessions
{
    public class GetMySessionsQuery : IRequest<IReadOnlyList<GetMySessionsResponse>>
    {
        public Guid ActorId { get; init; }
    }
}