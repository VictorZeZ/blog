namespace blog.Domain.Categories.Common
{
    public sealed record CategoryDailyCount(DateOnly Date, int CreatedCount, int DeletedCount);
}