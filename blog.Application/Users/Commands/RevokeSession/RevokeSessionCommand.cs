using MediatR;

namespace blog.Application.Users.Commands.RevokeSession
{
    public class RevokeSessionCommand : IRequest<RevokeSessionResponse>
    {
        public Guid ActorId { get; init; }
        public Guid SessionId { get; init; }
    }
}