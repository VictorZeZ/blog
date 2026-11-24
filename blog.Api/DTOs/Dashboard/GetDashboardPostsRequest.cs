using blog.Domain.Posts.Enums;

namespace blog.Api.DTOs.Dashboard
{
    public class GetDashboardPostsRequest
    {
        public string? Term { get; init; }
        public PostFilter Filter { get; init; } = PostFilter.All;
        public PostSortBy SortBy { get; init; } = PostSortBy.Newest;
        public Guid? CategoryId { get; init; }
        public Guid? AuthorId { get; init; }
    }
}