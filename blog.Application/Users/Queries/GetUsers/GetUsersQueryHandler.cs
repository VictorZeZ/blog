using blog.Domain.Common;
using blog.Domain.Users.Repository;
using MediatR;

namespace blog.Application.Users.Queries.GetUsers
{
    public class GetUsersQueryHandler(IUserRepository userRepository) : IRequestHandler<GetUsersQuery, PagedResult<GetUsersResponse>>
    {
        public async Task<PagedResult<GetUsersResponse>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
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
