using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;

namespace blog.Domain.Users.Extensions
{
    public static class UserRepositoryExtensions
    {
        public static async Task<bool> IsElevatedActorAsync(this IUserRepository userRepository, Guid? actorId, CancellationToken ct = default)
        {
            if (actorId is null)
                return false;

            var actor = await userRepository.GetByIdAsync(new UserId(actorId.Value), ct);
            return actor is not null && actor.IsElevated();
        }
    }
}