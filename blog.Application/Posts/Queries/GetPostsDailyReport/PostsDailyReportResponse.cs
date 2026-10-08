namespace blog.Application.Posts.Queries.GetPostsDailyReport
{
    public class PostsDailyReportResponse
    {
        public DateOnly From { get; init; }
        public DateOnly To { get; init; }
        public Guid? CategoryId { get; init; }
        public Guid? AuthorId { get; init; }
        public int TotalCount { get; init; }
        public PostStatusTotalsResponse StatusTotals { get; init; } = null!;
        public List<PostDailyCountResponse> Days { get; init; } = [];
    }
}