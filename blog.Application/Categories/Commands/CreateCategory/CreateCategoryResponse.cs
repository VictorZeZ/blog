namespace blog.Application.Categories.Commands.CreateCategory
{
    public class CreateCategoryResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
    }
}
