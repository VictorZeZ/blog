namespace blog.Domain.Posts.Types
{
    public readonly record struct PostId(Guid Value)
    {
        public static PostId New() => new(Guid.CreateVersion7());
        public static PostId Empty => new(Guid.Empty);

        public override string ToString() => Value.ToString();
    }
}
