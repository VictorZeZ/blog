namespace blog.Domain.Users.Common
{
    public class TopAuthorResult
    {
        public Guid AuthorId { get; init; }
        public string FullName { get; init; } = string.Empty;
        public int PublishedCount { get; init; }
        public int TotalViewCount { get; init; }
    }
}