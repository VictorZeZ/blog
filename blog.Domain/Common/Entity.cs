namespace blog.Domain.Common
{
    public abstract class Entity<TId> where TId : notnull
    {
        public TId Id { get; protected init; }

        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; private set; }

        protected Entity(TId id)
        {
            Id = id;
        }

        public void MarkAsUpdated()
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
