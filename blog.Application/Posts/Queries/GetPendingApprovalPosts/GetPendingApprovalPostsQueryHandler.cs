using blog.Domain.Common;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Repository;
using MediatR;

namespace blog.Application.Posts.Queries.GetPendingApprovalPosts
{
    public class GetPendingApprovalPostsQueryHandler(IPostRepository postRepository) : IRequestHandler<GetPendingApprovalPostsQuery, PagedResult<PostSummaryResponse>>
    {
        public async Task<PagedResult<PostSummaryResponse>> Handle(GetPendingApprovalPostsQuery request, CancellationToken cancellationToken)
        {
            return await postRepository.GetPendingApprovalAsync(request.Paging, request.SortBy, cancellationToken);
        }
    }
}