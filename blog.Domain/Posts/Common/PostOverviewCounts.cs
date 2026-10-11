namespace blog.Domain.Posts.Common
{
    public sealed record PostOverviewCounts(int DraftCount, int PendingApprovalCount, int PublishedCount, int RejectedCount, int TotalViewCount);
}