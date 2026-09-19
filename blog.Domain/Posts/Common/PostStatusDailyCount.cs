namespace blog.Domain.Posts.Common
{
    public sealed record PostStatusDailyCount(DateOnly Date, int DraftCount, int PendingApprovalCount, int PublishedCount, int RejectedCount);
}