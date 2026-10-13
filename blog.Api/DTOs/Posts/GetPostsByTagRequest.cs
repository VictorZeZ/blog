using blog.Domain.Posts.Enums;

namespace blog.Api.DTOs.Posts
{
    public class GetPostsByTagRequest
    {
        public List<string> Tags { get; init; } = [];
        public PostSortBy SortBy { get; init; } = PostSortBy.Newest;
        public PostTagGroupingMode GroupingMode { get; init; } = PostTagGroupingMode.None;
    }
}