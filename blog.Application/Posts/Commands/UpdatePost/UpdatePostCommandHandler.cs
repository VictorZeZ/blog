using blog.Domain.Common.Enum;
using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Entities;
using blog.Domain.Posts.Repository;
using blog.Domain.Posts.Types;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Posts.Commands.UpdatePost
{
    public class UpdatePostCommandHandler(IUserRepository userRepository, IPostRepository postRepository, IFileStorageService fileStorageService, IUnitOfWork unitOfWork) : IRequestHandler<UpdatePostCommand, UpdatePostResponse>
    {
        public async Task<UpdatePostResponse> Handle(UpdatePostCommand request, CancellationToken cancellationToken)
        {
            var actor = await userRepository.GetByIdAsync(new UserId(request.ActorId), cancellationToken);
            if (actor is null)
                throw new NotFoundException("User", request.ActorId);

            actor.EnsureActive();

            var post = await postRepository.GetByIdAsync(new PostId(request.PostId), cancellationToken);
            if (post is null)
                throw new NotFoundException("Post", request.PostId);

            if (!actor.CanManagePost(post.AuthorId))
                throw new ForbiddenException("update_post");

            var newSlug = Post.GenerateSlug(request.Title);
            if (newSlug != post.Slug)
            {
                var slugTaken = await postRepository.ExistsBySlugAsync(newSlug, cancellationToken);
                if (slugTaken)
                    throw new AlreadyExistsException("Post", request.Title);
            }

            var titleImageUrl = await ResolveTitleImageAsync(post.TitleImageUrl, request, cancellationToken);

            var requiresReapproval = !actor.IsElevated();

            post.Update(request.Title, titleImageUrl, request.Content, request.Tags, requiresReapproval);

            postRepository.Update(post);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdatePostResponse
            {
                Id = post.Id.Value,
                Title = post.Title,
                Slug = post.Slug,
                TitleImageUrl = post.TitleImageUrl,
                Status = post.Status
            };
        }

        private async Task<string?> ResolveTitleImageAsync(string? currentImageUrl, UpdatePostCommand request, CancellationToken ct)
        {
            if (request.RemoveTitleImage)
            {
                if (currentImageUrl is not null)
                    await fileStorageService.DeleteAsync(currentImageUrl, ct);

                return null;
            }

            if (request.TitleImageStream is null)
                return currentImageUrl;

            PostImageValidationRules.EnsureValid(
                request.TitleImageFileName!,
                request.TitleImageSizeBytes,
                request.TitleImageContentType!);

            var uploadedUrl = await fileStorageService.UploadAsync(request.TitleImageStream, request.TitleImageFileName!, StorageFolder.Posts, ct);

            if (currentImageUrl is not null)
                await fileStorageService.DeleteAsync(currentImageUrl, ct);

            return uploadedUrl;
        }
    }
}
