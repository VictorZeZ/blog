namespace blog.Application.Posts.Queries.GetPostsDailyReport
{
    public class PostStatusTotalsResponse
    {
        public int DraftCount { get; init; }
        public int PendingApprovalCount { get; init; }
        public int PublishedCount { get; init; }
        public int RejectedCount { get; init; }
    }
}