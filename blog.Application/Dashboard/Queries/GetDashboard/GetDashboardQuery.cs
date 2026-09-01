using MediatR;

namespace blog.Application.Dashboard.Queries.GetDashboard
{
    public class GetDashboardQuery : IRequest<GetDashboardResponse>
    {
        public Guid ActorId { get; init; }
    }
}
