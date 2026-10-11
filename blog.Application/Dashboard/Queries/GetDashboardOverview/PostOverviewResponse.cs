namespace blog.Application.Dashboard.Queries.GetDashboardOverview
{
    public class PostOverviewResponse
    {
        public int TotalCount { get; init; }
        public int DraftCount { get; init; }
        public int PendingApprovalCount { get; init; }
        public int PublishedCount { get; init; }
        public int RejectedCount { get; init; }
        public int TotalViewCount { get; init; }
    }
}