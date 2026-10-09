namespace blog.Application.Categories.Queries.GetCategoriesDailyReport
{
    public class CategoriesDailyReportResponse
    {
        public DateOnly From { get; init; }
        public DateOnly To { get; init; }
        public int CreatedCount { get; init; }
        public int DeletedCount { get; init; }
        public List<CategoryDailyCountResponse> Days { get; init; } = [];
    }
}