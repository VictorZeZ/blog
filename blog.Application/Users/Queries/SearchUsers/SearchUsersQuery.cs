using blog.Domain.Common;
using blog.Domain.Users.Common;
using MediatR;

namespace blog.Application.Users.Queries.SearchUsers
{
    public class SearchUsersQuery : IRequest<PagedResult<UserSearchResult>>
    {
        public string Term { get; init; } = string.Empty;
        public Guid? ActorId { get; init; }
        public PagedRequest Paging { get; init; } = new();
    }
}
