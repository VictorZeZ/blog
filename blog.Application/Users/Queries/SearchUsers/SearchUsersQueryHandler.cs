using blog.Domain.Common;
using blog.Domain.Users.Common;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Users.Queries.SearchUsers
{
    public class SearchUsersQueryHandler(IUserRepository userRepository) : IRequestHandler<SearchUsersQuery, PagedResult<UserSearchResult>>
    {
        public async Task<PagedResult<UserSearchResult>> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
        {
            var isElevatedActor = await IsElevatedActorAsync(request.ActorId, cancellationToken);

            return await userRepository.SearchAsync(request.Paging, request.Term, isElevatedActor, cancellationToken);
        }

        private async Task<bool> IsElevatedActorAsync(Guid? actorId, CancellationToken ct)
        {
            if (actorId is null)
                return false;

            var actor = await userRepository.GetByIdAsync(new UserId(actorId.Value), ct);
            return actor is not null && actor.IsElevated();
        }
    }
}
