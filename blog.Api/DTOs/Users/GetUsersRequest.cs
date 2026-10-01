using blog.Domain.Common.Enum;
using blog.Domain.Users.Enums;

namespace blog.Api.DTOs.Users
{
    public class GetUsersRequest
    {
        public UserSortBy SortBy { get; init; } = UserSortBy.Newest;
        public UserFilter Filter { get; init; } = UserFilter.All;
        public TriStateFilter Banned { get; init; } = TriStateFilter.All;
        public TriStateFilter Deleted { get; init; } = TriStateFilter.All;
        public TriStateFilter EmailConfirmed { get; init; } = TriStateFilter.All;
        public TriStateFilter TwoFactorEnabled { get; init; } = TriStateFilter.All;
    }
}