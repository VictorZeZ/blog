using MediatR;

namespace blog.Application.Posts.Commands.CreateDraft
{
    public class CreateDraftCommand : IRequest<CreateDraftResponse>
    {
        public Guid AuthorId { get; init; }
        public Guid CategoryId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Summary { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
        public List<string> Tags { get; init; } = [];
        public Stream? TitleImageStream { get; init; }
        public string? TitleImageFileName { get; init; }
        public string? TitleImageContentType { get; init; }
        public long TitleImageSizeBytes { get; init; }
    }
}