namespace blog.Application.Dashboard.Queries.GetDashboard
{
    public class GetDashboardResponse
    {
        public DashboardProfileResponse Profile { get; init; } = null!;
        public MyContentResponse MyContent { get; init; } = null!;
        public AuthorInsightsResponse? AuthorInsights { get; init; }
        public ModerationQueueResponse? ModerationQueue { get; init; }
        public PlatformStatsResponse? PlatformStats { get; init; }
        public OwnerOverviewResponse? OwnerOverview { get; init; }
    }
}
