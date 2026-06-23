using blog.Domain.Common;
using blog.Domain.Posts.Enums;
using blog.Domain.Posts.Types;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Enums;
using blog.Domain.Users.Types;

namespace blog.Domain.Posts.Entities
{
    public class Post : Entity<PostId>
    {
        public string Title { get; private set; }
        public string? TitleImageUrl { get; private set; }
        public string Content { get; private set; }
        public string Slug { get; private set; }
        public List<string> Tags { get; private set; } = [];
        public PostStatus Status { get; private set; }

        public UserId AuthorId { get; private set; }
        public User Author { get; private set; }

        private Post() : base(PostId.Empty) { }

        public int ViewCount { get; private set; }

        public Post(string title, string? titleImageUrl, string content, List<string> tags, User author) : base(PostId.New())
        {
            Title = title;
            TitleImageUrl = titleImageUrl;
            Content = content;
            Tags = tags;
            AuthorId = author.Id;
            Author = author;
            Slug = GenerateSlug(title);

            Status = author.Level >= UserLevel.Admin
                ? PostStatus.Published
                : PostStatus.PendingApproval;
        }

        public void Approve()
        {
            Status = PostStatus.Published;
            MarkAsUpdated();
        }

        public void Reject()
        {
            Status = PostStatus.Rejected;
            MarkAsUpdated();
        }

        public void Update(string title, string content, List<string> tags)
        {
            Title = title;
            Content = content;
            Tags = tags;
            Slug = GenerateSlug(title);
            MarkAsUpdated();
        }

        private static string GenerateSlug(string title)
        {
            return title
                .ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("_", "-");
        }

        public void IncrementView()
        {
            ViewCount++;
        }
    }
}
