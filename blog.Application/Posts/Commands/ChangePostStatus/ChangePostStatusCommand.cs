using blog.Domain.Common.Interfaces;
using blog.Domain.Posts.Enums;
using blog.Domain.Users.Enums;
using MediatR;

namespace blog.Application.Posts.Commands.ChangePostStatus
{
    public class ChangePostStatusCommand : IRequest<ChangePostStatusResponse>, IRequireActorLevel
    {
        public Guid ActorId { get; init; }
        public Guid PostId { get; init; }
        public ChangePostStatusAction Action { get; init; }

        public UserLevel MinimumLevel => UserLevel.Admin;
    }
}
