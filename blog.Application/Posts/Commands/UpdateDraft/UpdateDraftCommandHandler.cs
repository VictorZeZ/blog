using blog.Domain.Categories.Repository;
using blog.Domain.Categories.Types;
using blog.Domain.Common.Enum;
using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Entities;
using blog.Domain.Posts.Enums;
using blog.Domain.Posts.Repository;
using blog.Domain.Posts.Types;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Posts.Commands.UpdateDraft
{
    public class UpdateDraftCommandHandler(IUserRepository userRepository, IPostRepository postRepository, ICategoryRepository categoryRepository, IFileStorageService fileStorageService, IUnitOfWork unitOfWork) : IRequestHandler<UpdateDraftCommand, UpdateDraftResponse>
    {
        public async Task<UpdateDraftResponse> Handle(UpdateDraftCommand request, CancellationToken cancellationToken)
        {
            var actor = await userRepository.GetByIdAsync(new UserId(request.ActorId), cancellationToken);
            if (actor is null)
                throw new NotFoundException("User", request.ActorId);

            actor.EnsureActive();

            var post = await postRepository.GetByIdAsync(new PostId(request.PostId), cancellationToken);
            if (post is null)
                throw new NotFoundException("Post", request.PostId);

            if (!actor.CanManagePost(post.AuthorId))
                throw new ForbiddenException("update_draft");

            if (post.Status != PostStatus.Draft)
                throw new InvalidStateException("Post", post.Status.ToString(), "Draft");

            var categoryId = new CategoryId(request.CategoryId);
            var category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);
            if (category is null)
                throw new NotFoundException("Category", request.CategoryId);

            var newSlug = Post.GenerateSlug(request.Title);
            if (newSlug != post.Slug)
            {
                var slugTaken = await postRepository.ExistsBySlugAsync(newSlug, cancellationToken);
                if (slugTaken)
                    throw new AlreadyExistsException("Post", request.Title);
            }

            var titleImageUrl = await ResolveTitleImageAsync(post.TitleImageUrl, request, cancellationToken);

            // Status stays Draft regardless — Post.Update() only transitions Published -> PendingApproval,
            // so requiresReapproval has no effect here and is passed as false for clarity.
            post.Update(request.Title, request.Summary, titleImageUrl, request.Content, request.Tags, requiresReapproval: false, category);

            postRepository.Update(post);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new UpdateDraftResponse
            {
                Id = post.Id.Value,
                Title = post.Title,
                Summary = post.Summary,
                Slug = post.Slug,
                TitleImageUrl = post.TitleImageUrl,
                Status = post.Status
            };
        }

        private async Task<string?> ResolveTitleImageAsync(string? currentImageUrl, UpdateDraftCommand request, CancellationToken ct)
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

            await PostImageValidationRules.EnsureValidContentAsync(request.TitleImageStream, request.TitleImageContentType!, ct);

            var uploadedUrl = await fileStorageService.UploadAsync(request.TitleImageStream, request.TitleImageFileName!, StorageFolder.Posts, ct);

            if (currentImageUrl is not null)
                await fileStorageService.DeleteAsync(currentImageUrl, ct);

            return uploadedUrl;
        }
    }
}