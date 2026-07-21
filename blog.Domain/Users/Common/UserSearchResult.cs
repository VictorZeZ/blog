using blog.Domain.Users.Enums;

namespace blog.Domain.Users.Common
{
    public class UserSearchResult
    {
        public Guid Id { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string? Email { get; init; }
        public UserLevel Level { get; init; }
        public DateTime CreatedAt { get; init; }
        public int PublishedPostsCount { get; init; }
        public int TotalViewCount { get; init; }
    }
}
