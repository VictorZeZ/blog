namespace blog.Domain.Common
{
    public abstract class SoftDeletableEntity<TId> : Entity<TId> where TId : notnull
    {
        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; private set; }

        protected SoftDeletableEntity(TId id) : base(id) { }

        public virtual void SoftDelete()
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            MarkAsUpdated();
        }
    }
}
