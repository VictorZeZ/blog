namespace blog.Application.Categories.Queries.GetAllCategories
{
    public class GetAllCategoriesResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
    }
}
