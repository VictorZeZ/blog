namespace blog.Application.Posts.Queries.GetUserPostStatusReport
{
    public class GetUserPostStatusReportResponse
    {
        public Guid AuthorId { get; init; }
        public DateOnly From { get; init; }
        public DateOnly To { get; init; }
        public int DraftCount { get; init; }
        public int PendingApprovalCount { get; init; }
        public int PublishedCount { get; init; }
        public int RejectedCount { get; init; }
        public int TotalCount { get; init; }
    }
}
