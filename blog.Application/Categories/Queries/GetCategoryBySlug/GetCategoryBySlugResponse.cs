namespace blog.Application.Categories.Queries.GetCategoryBySlug
{
    public class GetCategoryBySlugResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
    }
}
