namespace blog.Application.Dashboard.Queries.GetDashboard
{
    public class MyContentResponse
    {
        public int DraftCount { get; init; }
        public int PendingApprovalCount { get; init; }
        public int PublishedCount { get; init; }
        public int RejectedCount { get; init; }
        public int TotalViewCount { get; init; }
    }
}
