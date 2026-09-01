namespace blog.Application.Dashboard.Queries.GetDashboard
{
    public class ModerationQueueResponse
    {
        public int PendingApprovalCount { get; init; }
        public int ActiveCategoryCount { get; init; }
    }
}
