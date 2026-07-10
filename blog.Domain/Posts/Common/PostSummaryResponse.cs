using blog.Domain.Posts.Enums;

namespace blog.Domain.Posts.Common
{
    public class PostSummaryResponse
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? TitleImageUrl { get; init; }
        public string Slug { get; init; } = string.Empty;
        public List<string> Tags { get; init; } = [];
        public PostStatus Status { get; init; }
        public int ViewCount { get; init; }
        public Guid AuthorId { get; init; }
        public string AuthorFullName { get; init; } = string.Empty;
        public string CategoryName { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }
}
