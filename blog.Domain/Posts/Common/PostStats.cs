using blog.Domain.Common;

namespace blog.Domain.Posts.Common
{
    public class PostStats
    {
        public int TotalCount { get; init; }
        public int DraftCount { get; init; }
        public int PendingApprovalCount { get; init; }
        public int PublishedCount { get; init; }
        public int RejectedCount { get; init; }
        public int TotalViewCount { get; init; }
        public IReadOnlyList<DailyCount> PostsPerDay { get; init; } = [];
    }
}
