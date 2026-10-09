using blog.Domain.Common.Enum;
using blog.Domain.Common.Interfaces;
using blog.Domain.Users.Enums;
using MediatR;

namespace blog.Application.Users.Queries.GetUsersDailyReport
{
    public class GetUsersDailyReportQuery : IRequest<UsersDailyReportResponse>, IRequireActorLevel
    {
        public Guid ActorId { get; init; }
        public DateOnly? From { get; init; }
        public DateOnly? To { get; init; }
        public TriStateFilter EmailConfirmed { get; init; } = TriStateFilter.All;
        public TriStateFilter TwoFactorEnabled { get; init; } = TriStateFilter.All;

        public UserLevel MinimumLevel => UserLevel.Admin;
    }
}