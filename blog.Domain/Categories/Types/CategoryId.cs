namespace blog.Domain.Categories.Types
{
    public readonly record struct CategoryId(Guid Value)
    {
        public static CategoryId New() => new(Guid.CreateVersion7());
        public static CategoryId Empty => new(Guid.Empty);

        public override string ToString() => Value.ToString();
    }
}
