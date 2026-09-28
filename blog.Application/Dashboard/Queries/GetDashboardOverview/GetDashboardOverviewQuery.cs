using blog.Domain.Common.Enum;
using blog.Domain.Posts.Enums;
using blog.Domain.Users.Enums;
using MediatR;

namespace blog.Application.Dashboard.Queries.GetDashboardOverview
{
    public class GetDashboardOverviewQuery : IRequest<GetDashboardOverviewResponse>
    {
        public Guid ActorId { get; init; }
        public PostFilter PostFilter { get; init; } = PostFilter.All;
        public UserFilter UserLevelFilter { get; init; } = UserFilter.All;
        public TriStateFilter UserBanned { get; init; } = TriStateFilter.All;
        public TriStateFilter UserDeleted { get; init; } = TriStateFilter.All;
        public TriStateFilter UserEmailConfirmed { get; init; } = TriStateFilter.All;
        public TriStateFilter UserTwoFactorEnabled { get; init; } = TriStateFilter.All;
        public TriStateFilter CategoryDeleted { get; init; } = TriStateFilter.All;
    }
}