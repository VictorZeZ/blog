namespace blog.Domain.Common.Interfaces
{
    public interface IRequirePasswordConfirmation
    {
        Guid UserId { get; }
        string CurrentPassword { get; }
        bool RequiresPasswordConfirmation { get; }
    }
}