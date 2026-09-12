namespace blog.Application.Categories.Queries.GetCategoryPerformanceReport
{
    public class GetCategoryPerformanceReportResponse
    {
        public DateOnly From { get; init; }
        public DateOnly To { get; init; }
        public IReadOnlyList<CategoryPerformanceItemResponse> Categories { get; init; } = [];
    }
}