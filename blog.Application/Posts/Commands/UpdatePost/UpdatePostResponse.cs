using blog.Domain.Posts.Enums;

namespace blog.Application.Posts.Commands.UpdatePost
{
    public class UpdatePostResponse
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
        public string? TitleImageUrl { get; init; }
        public PostStatus Status { get; init; }
    }
}
