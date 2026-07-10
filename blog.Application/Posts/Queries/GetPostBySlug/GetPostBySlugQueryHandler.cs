using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Enums;
using blog.Domain.Posts.Repository;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Posts.Queries.GetPostBySlug
{
    public class GetPostBySlugQueryHandler(IPostRepository postRepository, IUserRepository userRepository, IUnitOfWork unitOfWork) : IRequestHandler<GetPostBySlugQuery, GetPostBySlugResponse>
    {
        public async Task<GetPostBySlugResponse> Handle(GetPostBySlugQuery request, CancellationToken cancellationToken)
        {
            var post = await postRepository.GetBySlugAsync(request.Slug, cancellationToken);
            if (post is null)
                throw new NotFoundException("Post", request.Slug);

            var isPublished = post.Status == PostStatus.Published;

            if (!isPublished)
            {
                var canPreview = await CanPreviewUnpublishedAsync(request.ActorId, post.AuthorId, cancellationToken);
                if (!canPreview)
                    throw new NotFoundException("Post", request.Slug);
            }

            if (isPublished)
            {
                post.IncrementView();
                postRepository.Update(post);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return new GetPostBySlugResponse
            {
                Id = post.Id.Value,
                Title = post.Title,
                TitleImageUrl = post.TitleImageUrl,
                Content = post.Content,
                Slug = post.Slug,
                Tags = post.Tags,
                Status = post.Status,
                ViewCount = post.ViewCount,
                AuthorId = post.AuthorId.Value,
                AuthorFullName = post.Author.FullName,
                CategoryName = post.Category.Name,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt
            };
        }

        private async Task<bool> CanPreviewUnpublishedAsync(Guid? actorId, UserId authorId, CancellationToken ct)
        {
            if (actorId is null)
                return false;

            var actor = await userRepository.GetByIdAsync(new UserId(actorId.Value), ct);
            return actor is not null && actor.CanManagePost(authorId);
        }
    }
}
