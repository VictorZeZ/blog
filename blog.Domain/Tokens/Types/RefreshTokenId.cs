namespace blog.Domain.Tokens.Types
{
    public readonly record struct RefreshTokenId(Guid Value)
    {
        public static RefreshTokenId New() => new(Guid.CreateVersion7());
        public static RefreshTokenId Empty => new(Guid.Empty);

        public override string ToString() => Value.ToString();
    }
}
