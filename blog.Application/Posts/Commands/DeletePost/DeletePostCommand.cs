using MediatR;

namespace blog.Application.Posts.Commands.DeletePost
{
    public class DeletePostCommand : IRequest<DeletePostResponse>
    {
        public Guid ActorId { get; init; }
        public Guid PostId { get; init; }
    }
}
