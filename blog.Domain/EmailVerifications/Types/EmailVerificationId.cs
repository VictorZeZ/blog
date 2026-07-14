namespace blog.Domain.EmailVerifications.Types
{
    public readonly record struct EmailVerificationId(Guid Value)
    {
        public static EmailVerificationId New() => new(Guid.CreateVersion7());
        public static EmailVerificationId Empty => new(Guid.Empty);

        public override string ToString() => Value.ToString();
    }
}
