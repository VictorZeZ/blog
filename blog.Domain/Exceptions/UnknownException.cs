namespace blog.Domain.Exceptions
{
    public class UnknownException : DomainException
    {
        public UnknownException(string? context = null)
            : base(500, "UNKNOWN_ERROR", "An unknown error occurred", context is null ? null : new { Context = context })
        { }
    }
}
