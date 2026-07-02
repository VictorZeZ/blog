namespace blog.Api.DTOs.Posts
{
    public class UpdatePostRequest
    {
        public string Title { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
        public List<string> Tags { get; init; } = [];
        public IFormFile? TitleImage { get; init; }
        public bool RemoveTitleImage { get; init; }
    }
}
