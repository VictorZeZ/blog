using blog.Domain.Exceptions;
using blog.Domain.Users.Entities;
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

        public static async Task<UserId?> ResolveAuthorScopeAsync(this IUserRepository userRepository, User actor, Guid? requestedAuthorId, CancellationToken ct = default)
        {
            if (!actor.IsElevated())
                return actor.Id;

            if (requestedAuthorId is null)
                return null;

            var authorId = new UserId(requestedAuthorId.Value);
            var author = await userRepository.GetByIdAsync(authorId, ct);
            if (author is null)
                throw new NotFoundException("User", requestedAuthorId.Value);

            return authorId;
        }
    }
}