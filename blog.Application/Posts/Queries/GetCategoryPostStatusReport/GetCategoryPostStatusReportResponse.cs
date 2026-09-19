using blog.Domain.Posts.Common;

namespace blog.Application.Posts.Queries.GetCategoryPostStatusReport
{
    public class GetCategoryPostStatusReportResponse
    {
        public Guid CategoryId { get; init; }
        public DateOnly From { get; init; }
        public DateOnly To { get; init; }
        public int DraftCount { get; init; }
        public int PendingApprovalCount { get; init; }
        public int PublishedCount { get; init; }
        public int RejectedCount { get; init; }
        public int TotalCount { get; init; }
        public IReadOnlyList<PostStatusDailyCount> DailyBreakdown { get; init; } = [];
    }
}