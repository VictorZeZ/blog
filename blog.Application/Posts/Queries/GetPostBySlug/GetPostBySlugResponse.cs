using blog.Domain.Posts.Enums;

namespace blog.Application.Posts.Queries.GetPostBySlug
{
    public class GetPostBySlugResponse
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? TitleImageUrl { get; init; }
        public string Content { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
        public List<string> Tags { get; init; } = [];
        public PostStatus Status { get; init; }
        public int ViewCount { get; init; }
        public Guid AuthorId { get; init; }
        public string AuthorFullName { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
    }
}
