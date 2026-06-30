using blog.Domain.Posts.Enums;

namespace blog.Application.Posts.Commands.CreatePost
{
    public class CreatePostResponse
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
        public PostStatus Status { get; init; }
    }
}
