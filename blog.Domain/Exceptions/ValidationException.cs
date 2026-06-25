namespace blog.Domain.Exceptions
{
    public class ValidationException : DomainException
    {
        public ValidationException(string field, string reason)
            : base(422, "VALIDATION_ERROR", "Validation failed", new { Field = field, Reason = reason })
        { }

        public ValidationException(IEnumerable<object> errors)
            : base(422, "VALIDATION_ERROR", "Validation failed", new { Errors = errors })
        { }
    }
}
