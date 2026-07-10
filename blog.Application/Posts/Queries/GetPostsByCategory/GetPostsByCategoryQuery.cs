using blog.Domain.Common;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Enums;
using MediatR;

namespace blog.Application.Posts.Queries.GetPostsByCategory
{
    public class GetPostsByCategoryQuery : IRequest<PagedResult<PostSummaryResponse>>
    {
        public string CategorySlug { get; init; } = string.Empty;
        public PagedRequest Paging { get; init; } = new();
        public PostSortBy SortBy { get; init; } = PostSortBy.Newest;
    }
}
