namespace blog.Domain.Tokens.Types
{
    public readonly record struct SessionId(Guid Value)
    {
        public static SessionId New() => new(Guid.CreateVersion7());
        public static SessionId Empty => new(Guid.Empty);

        public override string ToString() => Value.ToString();
    }
}