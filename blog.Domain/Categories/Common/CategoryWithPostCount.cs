namespace blog.Domain.Categories.Common
{
    public sealed record CategoryWithPostCount(Guid Id, string Name, string Slug, bool IsDeleted, DateTime? DeletedAt, DateTime CreatedAt, int PostsCount);
}