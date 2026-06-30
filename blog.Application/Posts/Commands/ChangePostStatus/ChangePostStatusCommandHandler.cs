using blog.Domain.Common.Enum;
using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Enums;
using blog.Domain.Posts.Repository;
using blog.Domain.Posts.Types;
using blog.Domain.Users.Enums;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Posts.Commands.ChangePostStatus
{
    public class ChangePostStatusCommandHandler(IUserRepository userRepository, IPostRepository postRepository, IUnitOfWork unitOfWork) : IRequestHandler<ChangePostStatusCommand, ChangePostStatusResponse>
    {
        public async Task<ChangePostStatusResponse> Handle(ChangePostStatusCommand request, CancellationToken cancellationToken)
        {
            var actor = await userRepository.GetByIdAsync(new UserId(request.ActorId), cancellationToken);
            if (actor is null)
                throw new NotFoundException("User", request.ActorId);

            actor.EnsureActive();

            if (actor.Level != UserLevel.Admin && actor.Level != UserLevel.Owner)
                throw new ForbiddenException("change_post_status");

            var post = await postRepository.GetByIdAsync(new PostId(request.PostId), cancellationToken);
            if (post is null)
                throw new NotFoundException("Post", request.PostId);

            var targetStatus = request.Action switch
            {
                ChangePostStatusAction.Approve => PostStatus.Published,
                ChangePostStatusAction.Reject => PostStatus.Rejected,
                _ => throw new UnsupportedOperationException(request.Action.ToString())
            };

            if (post.Status == targetStatus)
                throw new InvalidStateException("Post", post.Status.ToString(), $"Different from {targetStatus}");

            switch (request.Action)
            {
                case ChangePostStatusAction.Approve:
                    post.Approve();
                    break;
                case ChangePostStatusAction.Reject:
                    post.Reject();
                    break;
            }

            postRepository.Update(post);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new ChangePostStatusResponse
            {
                Id = post.Id.Value,
                Title = post.Title,
                Status = post.Status
            };
        }
    }
}
