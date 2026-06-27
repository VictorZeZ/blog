using blog.Domain.Common;
using blog.Domain.Exceptions;
using blog.Domain.Users.Enums;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Users.Queries.GetUsers
{
    public class GetUsersQueryHandler(IUserRepository userRepository) : IRequestHandler<GetUsersQuery, PagedResult<GetUsersResponse>>
    {
        public async Task<PagedResult<GetUsersResponse>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            var actor = await userRepository.GetByIdAsync(new UserId(request.ActorId), cancellationToken);
            if (actor is null)
                throw new NotFoundException("User", request.ActorId);

            if (actor.IsDeleted)
                throw new InvalidStateException("User", "Deleted", "Active");

            if (actor.Level == UserLevel.Normal || actor.Level == UserLevel.Author)
                throw new ForbiddenException("get_users");

            var result = await userRepository.GetAllAsync(
                request.Paging,
                request.SortBy,
                request.Filter,
                cancellationToken);

            return new PagedResult<GetUsersResponse>(
                result.Items.Select(u => new GetUsersResponse
                {
                    Id = u.Id.Value,
                    Email = u.Email,
                    FullName = u.FullName,
                    Level = u.Level,
                    IsBanned = u.IsBanned,
                    IsDeleted = u.IsDeleted,
                    CreatedAt = u.CreatedAt
                }),
                result.TotalCount,
                result.Page,
                result.PageSize);
        }
    }
}
