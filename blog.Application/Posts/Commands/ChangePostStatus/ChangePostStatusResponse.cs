using blog.Domain.Posts.Enums;

namespace blog.Application.Posts.Commands.ChangePostStatus
{
    public class ChangePostStatusResponse
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public PostStatus Status { get; init; }
    }
}
