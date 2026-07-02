using blog.Domain.Posts.Enums;

namespace blog.Api.DTOs.Posts
{
    public class ChangePostStatusRequest
    {
        public ChangePostStatusAction Action { get; init; }
    }
}
