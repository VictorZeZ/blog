using MediatR;

namespace blog.Application.Posts.Commands.PublishDraft
{
    public class PublishDraftCommand : IRequest<PublishDraftResponse>
    {
        public Guid ActorId { get; init; }
        public Guid PostId { get; init; }
    }
}