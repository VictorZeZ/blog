using blog.Domain.Common;
using blog.Domain.Posts.Common;
using blog.Domain.Users.Common;

namespace blog.Application.Dashboard.Queries.GetDashboard
{
    public class PlatformStatsResponse
    {
        public int TotalUserCount { get; init; }
        public int BannedUserCount { get; init; }
        public int TotalPostCount { get; init; }
        public int TotalViewCount { get; init; }
        public IReadOnlyList<DailyCount> RegistrationsPerDay { get; init; } = [];
        public IReadOnlyList<PostSummaryResponse> TopPosts { get; init; } = [];
        public IReadOnlyList<TopAuthorResult> TopAuthors { get; init; } = [];
    }
}