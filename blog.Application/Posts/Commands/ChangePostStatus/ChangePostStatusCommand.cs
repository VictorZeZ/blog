using blog.Domain.Common.Enum;
using MediatR;

namespace blog.Application.Posts.Commands.ChangePostStatus
{
    public class ChangePostStatusCommand : IRequest<ChangePostStatusResponse>
    {
        public Guid ActorId { get; init; }
        public Guid PostId { get; init; }
        public ChangePostStatusAction Action { get; init; }
    }
}
