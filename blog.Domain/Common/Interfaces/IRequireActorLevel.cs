using blog.Domain.Users.Enums;

namespace blog.Domain.Common.Interfaces
{
    public interface IRequireActorLevel
    {
        Guid ActorId { get; }
        UserLevel MinimumLevel { get; }
    }
}
