namespace blog.Domain.Exceptions
{
    public class NotFoundException : DomainException
    {
        public NotFoundException(string resource, object id)
            : base(404, "NOT_FOUND", $"{resource} not found", new { Resource = resource, Id = id })
        { }
    }
}
