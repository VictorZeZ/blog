using MediatR;

namespace blog.Application.Posts.Queries.GetMyPostStatusReport
{
    public class GetMyPostStatusReportQuery : IRequest<GetMyPostStatusReportResponse>
    {
        public Guid ActorId { get; init; }
        public DateOnly? From { get; init; }
        public DateOnly? To { get; init; }
    }
}