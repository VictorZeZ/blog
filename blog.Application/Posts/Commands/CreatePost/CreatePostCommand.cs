using MediatR;

namespace blog.Application.Posts.Commands.CreatePost
{
    public class CreatePostCommand : IRequest<CreatePostResponse>
    {
        public Guid AuthorId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
        public List<string> Tags { get; init; } = [];
        public Stream? TitleImageStream { get; init; }
        public string? TitleImageFileName { get; init; }
        public string? TitleImageContentType { get; init; }
        public long TitleImageSizeBytes { get; init; }
    }
}
