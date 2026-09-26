namespace blog.Application.Dashboard.Queries.GetDashboard
{
    public class GetDashboardResponse
    {
        public DateOnly From { get; init; }
        public DateOnly To { get; init; }
        public DashboardProfileResponse Profile { get; init; } = null!;
        public MyContentResponse? MyContent { get; init; }
        public AuthorInsightsResponse? AuthorInsights { get; init; }
        public ModerationQueueResponse? ModerationQueue { get; init; }
        public SiteContentResponse? SiteContent { get; init; }
        public PlatformStatsResponse? PlatformStats { get; init; }
        public OwnerOverviewResponse? OwnerOverview { get; init; }
    }
}