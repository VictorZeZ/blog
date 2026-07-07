namespace blog.Domain.Exceptions
{
    public class LockedException : DomainException
    {
        public LockedException(string resource, DateTime lockedUntil)
            : base(423, "LOCKED", $"{resource} is locked", new { Resource = resource, LockedUntil = lockedUntil })
        { }
    }
}
