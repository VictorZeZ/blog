using blog.Domain.Common;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Enums;
using MediatR;

namespace blog.Application.Posts.Queries.SearchPosts
{
    public class SearchPostsQuery : IRequest<PagedResult<PostSummaryResponse>>
    {
        public string Term { get; init; } = string.Empty;
        public PagedRequest Paging { get; init; } = new();
        public PostSortBy SortBy { get; init; } = PostSortBy.Newest;
    }
}
