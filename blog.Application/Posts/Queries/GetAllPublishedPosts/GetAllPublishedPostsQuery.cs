using blog.Domain.Common;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Enums;
using MediatR;

namespace blog.Application.Posts.Queries.GetAllPublishedPosts
{
    public class GetAllPublishedPostsQuery : IRequest<PagedResult<PostSummaryResponse>>
    {
        public PagedRequest Paging { get; init; } = new();
        public PostSortBy SortBy { get; init; } = PostSortBy.Newest;
    }
}
