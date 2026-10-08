using MediatR;

namespace blog.Application.Posts.Queries.GetPostsDailyReport
{
    public class GetPostsDailyReportQuery : IRequest<PostsDailyReportResponse>
    {
        public Guid ActorId { get; init; }
        public DateOnly? From { get; init; }
        public DateOnly? To { get; init; }
        public Guid? CategoryId { get; init; }
        public Guid? AuthorId { get; init; }
    }
}