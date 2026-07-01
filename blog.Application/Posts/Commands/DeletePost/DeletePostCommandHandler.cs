using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Repository;
using blog.Domain.Posts.Types;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Posts.Commands.DeletePost
{
    public class DeletePostCommandHandler(IUserRepository userRepository, IPostRepository postRepository, IFileStorageService fileStorageService, IUnitOfWork unitOfWork) : IRequestHandler<DeletePostCommand, DeletePostResponse>
    {
        public async Task<DeletePostResponse> Handle(DeletePostCommand request, CancellationToken cancellationToken)
        {
            var actor = await userRepository.GetByIdAsync(new UserId(request.ActorId), cancellationToken);
            if (actor is null)
                throw new NotFoundException("User", request.ActorId);

            actor.EnsureActive();

            var post = await postRepository.GetByIdAsync(new PostId(request.PostId), cancellationToken);
            if (post is null)
                throw new NotFoundException("Post", request.PostId);

            if (!actor.CanManagePost(post.AuthorId))
                throw new ForbiddenException("delete_post");

            if (post.TitleImageUrl is not null)
                await fileStorageService.DeleteAsync(post.TitleImageUrl, cancellationToken);

            postRepository.Delete(post);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new DeletePostResponse { Success = true };
        }
    }
}
