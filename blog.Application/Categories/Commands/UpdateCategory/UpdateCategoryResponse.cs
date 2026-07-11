namespace blog.Application.Categories.Commands.UpdateCategory
{
    public class UpdateCategoryResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
    }
}
