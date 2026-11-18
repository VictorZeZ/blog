namespace blog.Application.Categories.Queries.GetCategories
{
    public class GetCategoriesResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
        public bool IsDeleted { get; init; }
        public DateTime? DeletedAt { get; init; }
        public DateTime CreatedAt { get; init; }
        public int PostsCount { get; init; }
    }
}