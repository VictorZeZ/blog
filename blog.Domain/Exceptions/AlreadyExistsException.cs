namespace blog.Domain.Exceptions
{
    public class AlreadyExistsException : DomainException
    {
        public AlreadyExistsException(string resource, object id)
            : base(409, "ALREADY_EXISTS", $"{resource} already exists", new { Resource = resource, Id = id })
        { }

        public AlreadyExistsException(string resource)
            : base(409, "ALREADY_EXISTS", $"{resource} already exists", new { Resource = resource })
        { }
    }
}