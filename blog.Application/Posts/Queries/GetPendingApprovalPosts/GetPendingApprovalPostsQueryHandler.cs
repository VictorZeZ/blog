using blog.Domain.Common;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Extensions;
using blog.Domain.Posts.Repository;
using MediatR;

namespace blog.Application.Posts.Queries.GetPendingApprovalPosts
{
    public class GetPendingApprovalPostsQueryHandler(IPostRepository postRepository) : IRequestHandler<GetPendingApprovalPostsQuery, PagedResult<PostSummaryResponse>>
    {
        public async Task<PagedResult<PostSummaryResponse>> Handle(GetPendingApprovalPostsQuery request, CancellationToken cancellationToken)
        {
            var result = await postRepository.GetPendingApprovalAsync(request.Paging, request.SortBy, cancellationToken);

            return new PagedResult<PostSummaryResponse>(
                result.Items.Select(p => p.ToSummaryResponse()),
                result.TotalCount,
                result.Page,
                result.PageSize);
        }
    }
}
