namespace blog.Domain.Exceptions
{
    public class UnavailableException : DomainException
    {
        public UnavailableException(string resource)
            : base(503, "UNAVAILABLE", $"{resource} is currently unavailable", new { Resource = resource })
        { }
    }
}
