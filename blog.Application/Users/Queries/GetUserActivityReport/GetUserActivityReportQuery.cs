using blog.Domain.Common.Interfaces;
using blog.Domain.Users.Enums;
using MediatR;

namespace blog.Application.Users.Queries.GetUserActivityReport
{
    public class GetUserActivityReportQuery : IRequest<GetUserActivityReportResponse>, IRequireActorLevel
    {
        public Guid ActorId { get; init; }
        public DateOnly? From { get; init; }
        public DateOnly? To { get; init; }

        public UserLevel MinimumLevel => UserLevel.Admin;
    }
}