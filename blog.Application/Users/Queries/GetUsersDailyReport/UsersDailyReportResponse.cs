using blog.Domain.Common.Enum;

namespace blog.Application.Users.Queries.GetUsersDailyReport
{
    public class UsersDailyReportResponse
    {
        public DateOnly From { get; init; }
        public DateOnly To { get; init; }
        public TriStateFilter EmailConfirmed { get; init; }
        public TriStateFilter TwoFactorEnabled { get; init; }
        public int RegisteredCount { get; init; }
        public int BannedCount { get; init; }
        public int DeletedCount { get; init; }
        public UserLevelTotalsResponse LevelTotals { get; init; } = null!;
        public List<UserDailyCountResponse> Days { get; init; } = [];
    }
}