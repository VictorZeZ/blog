using blog.Domain.Common;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Extensions;
using blog.Domain.Posts.Repository;
using MediatR;

namespace blog.Application.Posts.Queries.GetPostsByTag
{
    public class GetPostsByTagQueryHandler(IPostRepository postRepository) : IRequestHandler<GetPostsByTagQuery, PagedResult<PostSummaryResponse>>
    {
        public async Task<PagedResult<PostSummaryResponse>> Handle(GetPostsByTagQuery request, CancellationToken cancellationToken)
        {
            var result = await postRepository.GetByTagAsync(request.Paging, request.Tag, request.SortBy, cancellationToken);

            return new PagedResult<PostSummaryResponse>(
                result.Items.Select(p => p.ToSummaryResponse()),
                result.TotalCount,
                result.Page,
                result.PageSize);
        }
    }
}
