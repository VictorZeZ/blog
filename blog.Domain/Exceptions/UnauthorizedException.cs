namespace blog.Domain.Exceptions
{
    public class UnauthorizedException : DomainException
    {
        public UnauthorizedException()
            : base(401, "UNAUTHORIZED", "Authentication required")
        { }
    }
}
