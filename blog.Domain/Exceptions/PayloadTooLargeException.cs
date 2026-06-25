namespace blog.Domain.Exceptions
{
    public class PayloadTooLargeException : DomainException
    {
        public PayloadTooLargeException(string field, long maxSizeBytes)
            : base(413, "PAYLOAD_TOO_LARGE", $"{field} exceeds the maximum allowed size", new { Field = field, MaxSizeBytes = maxSizeBytes })
        { }
    }
}
