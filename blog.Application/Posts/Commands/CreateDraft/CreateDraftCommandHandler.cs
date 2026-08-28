using blog.Domain.Categories.Repository;
using blog.Domain.Categories.Types;
using blog.Domain.Common.Enum;
using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Entities;
using blog.Domain.Posts.Repository;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Posts.Commands.CreateDraft
{
    public class CreateDraftCommandHandler(IUserRepository userRepository, IPostRepository postRepository, ICategoryRepository categoryRepository, IFileStorageService fileStorageService, IUnitOfWork unitOfWork) : IRequestHandler<CreateDraftCommand, CreateDraftResponse>
    {
        private const int MaxDraftsPerUser = 10;

        public async Task<CreateDraftResponse> Handle(CreateDraftCommand request, CancellationToken cancellationToken)
        {
            var author = await userRepository.GetByIdAsync(new UserId(request.AuthorId), cancellationToken);
            if (author is null)
                throw new NotFoundException("User", request.AuthorId);

            author.EnsureActive();

            var draftCount = await postRepository.CountDraftsByAuthorAsync(author.Id, cancellationToken);
            if (draftCount >= MaxDraftsPerUser)
                throw new ValidationException("Drafts", $"You can have at most {MaxDraftsPerUser} draft posts. Delete or publish an existing draft first.");

            var categoryId = new CategoryId(request.CategoryId);
            var category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);
            if (category is null)
                throw new NotFoundException("Category", request.CategoryId);

            var newSlug = Post.GenerateSlug(request.Title);
            var slugTaken = await postRepository.ExistsBySlugAsync(newSlug, cancellationToken);
            if (slugTaken)
                throw new AlreadyExistsException("Post", request.Title);

            string? titleImageUrl = null;
            if (request.TitleImageStream is not null)
            {
                PostImageValidationRules.EnsureValid(request.TitleImageFileName!, request.TitleImageSizeBytes, request.TitleImageContentType!);
                await PostImageValidationRules.EnsureValidContentAsync(request.TitleImageStream, request.TitleImageContentType!, cancellationToken);

                titleImageUrl = await fileStorageService.UploadAsync(request.TitleImageStream, request.TitleImageFileName!, StorageFolder.Posts, cancellationToken);
            }

            var post = Post.CreateDraft(
                request.Title,
                request.Summary,
                titleImageUrl,
                request.Content,
                request.Tags,
                author,
                category);

            await postRepository.AddAsync(post, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateDraftResponse
            {
                Id = post.Id.Value,
                Title = post.Title,
                Summary = post.Summary,
                Slug = post.Slug,
                Status = post.Status
            };
        }
    }
}