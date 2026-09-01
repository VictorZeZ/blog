using blog.Domain.Common;

namespace blog.Application.Dashboard.Queries.GetDashboard
{
    public class AuthorInsightsResponse
    {
        public IReadOnlyList<DailyCount> PostsPerDay { get; init; } = [];
    }
}
