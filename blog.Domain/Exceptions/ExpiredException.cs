namespace blog.Domain.Exceptions
{
    public class ExpiredException : DomainException
    {
        public ExpiredException(string resource)
            : base(410, "EXPIRED", $"{resource} has expired", new { Resource = resource })
        { }
    }
}
