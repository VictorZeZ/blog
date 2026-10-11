using MediatR;

namespace blog.Application.Dashboard.Queries.GetDashboardOverview
{
    public class GetDashboardOverviewQuery : IRequest<GetDashboardOverviewResponse>
    {
        public Guid ActorId { get; init; }
    }
}