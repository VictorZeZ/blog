namespace blog.Application.Dashboard.Queries.GetDashboardOverview
{
    public class CategoryOverviewResponse
    {
        public int TotalCount { get; init; }
        public int ActiveCount { get; init; }
        public int DeletedCount { get; init; }
    }
}