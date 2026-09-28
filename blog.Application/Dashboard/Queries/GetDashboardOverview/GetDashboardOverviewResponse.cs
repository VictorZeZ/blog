namespace blog.Application.Dashboard.Queries.GetDashboardOverview
{
    public class GetDashboardOverviewResponse
    {
        public DashboardProfileResponse Profile { get; init; } = null!;
        public int MyPostsCount { get; init; }
        public SiteOverviewResponse? Site { get; init; }
    }
}