using blog.Domain.Common;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Extensions;
using blog.Domain.Posts.Repository;
using MediatR;

namespace blog.Application.Posts.Queries.GetAllPublishedPosts
{
    public class GetAllPublishedPostsQueryHandler(IPostRepository postRepository) : IRequestHandler<GetAllPublishedPostsQuery, PagedResult<PostSummaryResponse>>
    {
        public async Task<PagedResult<PostSummaryResponse>> Handle(GetAllPublishedPostsQuery request, CancellationToken cancellationToken)
        {
            var result = await postRepository.GetAllPublishedAsync(request.Paging, request.SortBy, cancellationToken);

            return new PagedResult<PostSummaryResponse>(
                result.Items.Select(p => p.ToSummaryResponse()),
                result.TotalCount,
                result.Page,
                result.PageSize);
        }
    }
}
