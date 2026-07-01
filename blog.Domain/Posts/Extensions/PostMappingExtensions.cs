using blog.Domain.Posts.Common;
using blog.Domain.Posts.Entities;

namespace blog.Domain.Posts.Extensions
{
    public static class PostMappingExtensions
    {
        public static PostSummaryResponse ToSummaryResponse(this Post post) => new()
        {
            Id = post.Id.Value,
            Title = post.Title,
            TitleImageUrl = post.TitleImageUrl,
            Slug = post.Slug,
            Tags = post.Tags,
            Status = post.Status,
            ViewCount = post.ViewCount,
            AuthorId = post.AuthorId.Value,
            AuthorFullName = post.Author.FullName,
            CreatedAt = post.CreatedAt
        };
    }
}
