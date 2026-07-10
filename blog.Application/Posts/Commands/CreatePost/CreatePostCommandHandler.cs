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

namespace blog.Application.Posts.Commands.CreatePost
{
    public class CreatePostCommandHandler(IUserRepository userRepository, IPostRepository postRepository, ICategoryRepository categoryRepository, IFileStorageService fileStorageService, IUnitOfWork unitOfWork) : IRequestHandler<CreatePostCommand, CreatePostResponse>
    {
        public async Task<CreatePostResponse> Handle(CreatePostCommand request, CancellationToken cancellationToken)
        {
            var author = await userRepository.GetByIdAsync(new UserId(request.AuthorId), cancellationToken);
            if (author is null)
                throw new NotFoundException("User", request.AuthorId);

            author.EnsureActive();

            if (!author.IsAuthorOrHigher())
                throw new ForbiddenException("create_post");

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

            var post = new Post(
                request.Title,
                titleImageUrl,
                request.Content,
                request.Tags,
                author,
                category);

            await postRepository.AddAsync(post, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreatePostResponse
            {
                Id = post.Id.Value,
                Title = post.Title,
                Slug = post.Slug,
                Status = post.Status
            };
        }
    }
}
