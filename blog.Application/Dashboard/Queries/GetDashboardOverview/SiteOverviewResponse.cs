namespace blog.Application.Dashboard.Queries.GetDashboardOverview
{
    public class SiteOverviewResponse
    {
        public int PostsCount { get; init; }
        public int UsersCount { get; init; }
        public int CategoriesCount { get; init; }
    }
}