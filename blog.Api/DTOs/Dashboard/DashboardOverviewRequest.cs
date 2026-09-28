using blog.Domain.Common.Enum;
using blog.Domain.Posts.Enums;
using blog.Domain.Users.Enums;

namespace blog.Api.DTOs.Dashboard
{
    public class DashboardOverviewRequest
    {
        public PostFilter PostFilter { get; init; } = PostFilter.All;
        public UserFilter UserLevelFilter { get; init; } = UserFilter.All;
        public TriStateFilter UserBanned { get; init; } = TriStateFilter.All;
        public TriStateFilter UserDeleted { get; init; } = TriStateFilter.All;
        public TriStateFilter UserEmailConfirmed { get; init; } = TriStateFilter.All;
        public TriStateFilter UserTwoFactorEnabled { get; init; } = TriStateFilter.All;
        public TriStateFilter CategoryDeleted { get; init; } = TriStateFilter.All;
    }
}