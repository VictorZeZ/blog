using blog.Domain.Posts.Enums;

namespace blog.Api.DTOs.Posts
{
    public class SearchPostsRequest
    {
        public string Term { get; init; } = string.Empty;
        public PostSortBy SortBy { get; init; } = PostSortBy.Newest;
    }
}