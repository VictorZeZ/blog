namespace blog.Application.Dashboard.Queries.GetDashboardOverview
{
    public class GetDashboardOverviewResponse
    {
        public DashboardProfileResponse Profile { get; init; } = null!;
        public PostOverviewResponse MyPosts { get; init; } = null!;
        public SiteOverviewResponse? Site { get; init; }
    }
}