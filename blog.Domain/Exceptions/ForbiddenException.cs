namespace blog.Domain.Exceptions
{
    public class ForbiddenException : DomainException
    {
        public ForbiddenException(string action)
            : base(403, "FORBIDDEN", "Access denied", new { Action = action })
        { }
    }
}
