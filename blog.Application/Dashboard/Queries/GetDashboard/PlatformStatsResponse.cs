using blog.Domain.Common;

namespace blog.Application.Dashboard.Queries.GetDashboard
{
    public class PlatformStatsResponse
    {
        public int TotalUserCount { get; init; }
        public int BannedUserCount { get; init; }
        public int TotalPostCount { get; init; }
        public int TotalViewCount { get; init; }
        public IReadOnlyList<DailyCount> RegistrationsPerDay { get; init; } = [];
    }
}
