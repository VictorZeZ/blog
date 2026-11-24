using blog.Domain.Common;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Enums;
using MediatR;

namespace blog.Application.Posts.Queries.GetDashboardPosts
{
    public class GetDashboardPostsQuery : IRequest<PagedResult<PostSummaryResponse>>
    {
        public Guid ActorId { get; init; }
        public string? Term { get; init; }
        public PagedRequest Paging { get; init; } = new();
        public PostSortBy SortBy { get; init; } = PostSortBy.Newest;
        public PostFilter Filter { get; init; } = PostFilter.All;
        public Guid? CategoryId { get; init; }
        public Guid? AuthorId { get; init; }
    }
}