namespace blog.Application.Categories.Queries.GetCategoriesDailyReport
{
    public class CategoryDailyCountResponse
    {
        public DateOnly Date { get; init; }
        public int CreatedCount { get; init; }
        public int DeletedCount { get; init; }
    }
}