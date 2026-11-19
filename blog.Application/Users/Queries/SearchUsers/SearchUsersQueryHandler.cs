using blog.Domain.Common;
using blog.Domain.Users.Common;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using MediatR;

namespace blog.Application.Users.Queries.SearchUsers
{
    public class SearchUsersQueryHandler(IUserRepository userRepository) : IRequestHandler<SearchUsersQuery, PagedResult<UserSearchResult>>
    {
        public async Task<PagedResult<UserSearchResult>> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
        {
            var isElevatedActor = await userRepository.IsElevatedActorAsync(request.ActorId, cancellationToken);

            return await userRepository.SearchAsync(request.Paging, request.Term, isElevatedActor, cancellationToken);
        }
    }
}