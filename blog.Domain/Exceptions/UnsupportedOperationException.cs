namespace blog.Domain.Exceptions
{
    public class UnsupportedOperationException : DomainException
    {
        public UnsupportedOperationException(string operation)
            : base(400, "UNSUPPORTED_OPERATION", $"{operation} is not supported", new { Operation = operation })
        { }
    }
}
