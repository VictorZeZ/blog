using blog.Domain.Dashboard.Enums;
using MediatR;

namespace blog.Application.Dashboard.Queries.GetDashboard
{
    public class GetDashboardQuery : IRequest<GetDashboardResponse>
    {
        public Guid ActorId { get; init; }
        public DashboardScope Scope { get; init; } = DashboardScope.Platform;
        public DateOnly? From { get; init; }
        public DateOnly? To { get; init; }
    }
}