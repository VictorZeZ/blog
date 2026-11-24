using blog.Domain.Categories.Repository;
using blog.Domain.Categories.Types;
using blog.Domain.Common;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Repository;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Posts.Queries.GetDashboardPosts
{
    public class GetDashboardPostsQueryHandler(IPostRepository postRepository, ICategoryRepository categoryRepository, IUserRepository userRepository) : IRequestHandler<GetDashboardPostsQuery, PagedResult<PostSummaryResponse>>
    {
        public async Task<PagedResult<PostSummaryResponse>> Handle(GetDashboardPostsQuery request, CancellationToken cancellationToken)
        {
            var actor = await userRepository.GetByIdAsync(new UserId(request.ActorId), cancellationToken);
            if (actor is null)
                throw new NotFoundException("User", request.ActorId);

            actor.EnsureActive();

            var authorId = await userRepository.ResolveAuthorScopeAsync(actor, request.AuthorId, cancellationToken);

            CategoryId? categoryId = null;
            if (request.CategoryId is not null)
            {
                categoryId = new CategoryId(request.CategoryId.Value);
                var category = await categoryRepository.GetByIdAsync(categoryId.Value, cancellationToken);
                if (category is null)
                    throw new NotFoundException("Category", request.CategoryId.Value);
            }

            return await postRepository.GetAllAsync(
                request.Paging,
                request.SortBy,
                request.Filter,
                includeDrafts: actor.IsOwner(),
                term: request.Term,
                categoryId: categoryId,
                authorId: authorId,
                visibleDraftsAuthorId: actor.Id,
                ct: cancellationToken);
        }
    }
}