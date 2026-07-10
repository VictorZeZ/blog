using blog.Domain.Common;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Extensions;
using blog.Domain.Posts.Repository;
using MediatR;

namespace blog.Application.Posts.Queries.GetPostsByCategory
{
    public class GetPostsByCategoryQueryHandler(IPostRepository postRepository) : IRequestHandler<GetPostsByCategoryQuery, PagedResult<PostSummaryResponse>>
    {
        public async Task<PagedResult<PostSummaryResponse>> Handle(GetPostsByCategoryQuery request, CancellationToken cancellationToken)
        {
            var result = await postRepository.GetByCategorySlugAsync(request.Paging, request.CategorySlug, request.SortBy, cancellationToken);

            return new PagedResult<PostSummaryResponse>(
                result.Items.Select(p => p.ToSummaryResponse()),
                result.TotalCount,
                result.Page,
                result.PageSize);
        }
    }
}
