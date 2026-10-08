namespace blog.Application.Posts.Queries.GetPostsDailyReport
{
    public class PostDailyCountResponse
    {
        public DateOnly Date { get; init; }
        public int DraftCount { get; init; }
        public int PendingApprovalCount { get; init; }
        public int PublishedCount { get; init; }
        public int RejectedCount { get; init; }
        public int TotalCount { get; init; }
    }
}