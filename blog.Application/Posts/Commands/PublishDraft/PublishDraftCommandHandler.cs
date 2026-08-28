using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Repository;
using blog.Domain.Posts.Types;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Posts.Commands.PublishDraft
{
    public class PublishDraftCommandHandler(IUserRepository userRepository, IPostRepository postRepository, IUnitOfWork unitOfWork) : IRequestHandler<PublishDraftCommand, PublishDraftResponse>
    {
        public async Task<PublishDraftResponse> Handle(PublishDraftCommand request, CancellationToken cancellationToken)
        {
            var actor = await userRepository.GetByIdAsync(new UserId(request.ActorId), cancellationToken);
            if (actor is null)
                throw new NotFoundException("User", request.ActorId);

            actor.EnsureActive();

            var post = await postRepository.GetByIdAsync(new PostId(request.PostId), cancellationToken);
            if (post is null)
                throw new NotFoundException("Post", request.PostId);

            if (!actor.CanManagePost(post.AuthorId))
                throw new ForbiddenException("publish_draft");

            post.PublishFromDraft();

            postRepository.Update(post);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new PublishDraftResponse
            {
                Id = post.Id.Value,
                Title = post.Title,
                Slug = post.Slug,
                Status = post.Status
            };
        }
    }
}