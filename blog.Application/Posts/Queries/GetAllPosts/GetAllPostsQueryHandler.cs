using blog.Domain.Common;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Enums;
using blog.Domain.Posts.Extensions;
using blog.Domain.Posts.Repository;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Posts.Queries.GetAllPosts
{
    public class GetAllPostsQueryHandler(IPostRepository postRepository, IUserRepository userRepository) : IRequestHandler<GetAllPostsQuery, PagedResult<PostSummaryResponse>>
    {
        public async Task<PagedResult<PostSummaryResponse>> Handle(GetAllPostsQuery request, CancellationToken cancellationToken)
        {
            var actor = await userRepository.GetByIdAsync(new UserId(request.ActorId), cancellationToken);
            if (actor is null)
                throw new NotFoundException("User", request.ActorId);

            if (request.Filter == PostFilter.Draft && !actor.IsOwner())
                throw new ForbiddenException("view_all_drafts");

            var result = await postRepository.GetAllAsync(request.Paging, request.SortBy, request.Filter, includeDrafts: actor.IsOwner(), cancellationToken);

            return new PagedResult<PostSummaryResponse>(
                result.Items.Select(p => p.ToSummaryResponse()),
                result.TotalCount,
                result.Page,
                result.PageSize);
        }
    }
}