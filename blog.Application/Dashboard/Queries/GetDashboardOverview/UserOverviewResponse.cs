using blog.Application.Users.Queries.GetUsersDailyReport;

namespace blog.Application.Dashboard.Queries.GetDashboardOverview
{
    public class UserOverviewResponse
    {
        public int TotalCount { get; init; }
        public UserLevelTotalsResponse Levels { get; init; } = null!;
        public int ActiveCount { get; init; }
        public int BannedCount { get; init; }
        public int DeletedCount { get; init; }
        public int EmailConfirmedCount { get; init; }
        public int EmailUnconfirmedCount { get; init; }
        public int TwoFactorEnabledCount { get; init; }
    }
}