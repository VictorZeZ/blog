namespace blog.Application.Categories.Queries.GetDeletedCategories
{
    public class GetDeletedCategoriesResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
        public DateTime? DeletedAt { get; init; }
    }
}