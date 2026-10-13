using blog.Domain.Posts.Enums;

namespace blog.Api.DTOs.Posts
{
    public class GetAllPostsRequest
    {
        public PostSortBy SortBy { get; init; } = PostSortBy.Newest;
        public PostFilter Filter { get; init; } = PostFilter.All;
    }
}