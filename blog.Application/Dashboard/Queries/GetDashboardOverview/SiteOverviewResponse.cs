namespace blog.Application.Dashboard.Queries.GetDashboardOverview
{
    public class SiteOverviewResponse
    {
        public PostOverviewResponse Posts { get; init; } = null!;
        public UserOverviewResponse Users { get; init; } = null!;
        public CategoryOverviewResponse Categories { get; init; } = null!;
    }
}