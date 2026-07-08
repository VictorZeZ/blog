namespace blog.Api.DTOs.Posts
{
    public class CreatePostRequest
    {
        public Guid CategoryId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
        public List<string> Tags { get; init; } = [];
        public IFormFile? TitleImage { get; init; }
    }
}
