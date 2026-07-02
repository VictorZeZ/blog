using blog.Domain.Common;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Enums;
using MediatR;

namespace blog.Application.Posts.Queries.GetAllPosts
{
    public class GetAllPostsQuery : IRequest<PagedResult<PostSummaryResponse>>
    {
        public Guid ActorId { get; init; }
        public PagedRequest Paging { get; init; } = new();
        public PostSortBy SortBy { get; init; } = PostSortBy.Newest;
        public PostFilter Filter { get; init; } = PostFilter.All;
    }
}
