using blog.Domain.Common;
using blog.Domain.Users.Enums;
using MediatR;

namespace blog.Application.Users.Queries.GetUsers
{
    public class GetUsersQuery : IRequest<PagedResult<GetUsersResponse>>
    {
        public Guid ActorId { get; init; }
        public PagedRequest Paging { get; init; } = new();
        public UserSortBy SortBy { get; init; } = UserSortBy.Newest;
        public UserFilter Filter { get; init; } = UserFilter.All;
    }
}
