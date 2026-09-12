namespace blog.Domain.Categories.Common
{
    public class CategoryPerformanceResult
    {
        public Guid CategoryId { get; init; }
        public string Name { get; init; } = string.Empty;
        public int DraftCount { get; init; }
        public int PendingApprovalCount { get; init; }
        public int PublishedCount { get; init; }
        public int RejectedCount { get; init; }
        public int TotalCount { get; init; }
        public int TotalViewCount { get; init; }
    }
}