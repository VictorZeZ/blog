namespace blog.Application.Categories.Queries.RestoreCategory
{
    public class RestoreCategoryResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
    }
}