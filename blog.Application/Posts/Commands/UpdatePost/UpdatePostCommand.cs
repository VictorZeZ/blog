using MediatR;

namespace blog.Application.Posts.Commands.UpdatePost
{
    public class UpdatePostCommand : IRequest<UpdatePostResponse>
    {
        public Guid ActorId { get; init; }
        public Guid PostId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
        public List<string> Tags { get; init; } = [];
        public Stream? TitleImageStream { get; init; }
        public string? TitleImageFileName { get; init; }
        public bool RemoveTitleImage { get; init; }
        public string? TitleImageContentType { get; init; }
        public long TitleImageSizeBytes { get; init; }
    }
}
