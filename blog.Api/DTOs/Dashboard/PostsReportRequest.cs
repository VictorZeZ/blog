namespace blog.Api.DTOs.Dashboard
{
    public class PostsReportRequest : ReportDateRangeRequest
    {
        public Guid? CategoryId { get; init; }
        public Guid? AuthorId { get; init; }
    }
}