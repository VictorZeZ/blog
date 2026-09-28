namespace blog.Api.DTOs.Dashboard
{
    public class ReportDateRangeRequest
    {
        public DateOnly? From { get; init; }
        public DateOnly? To { get; init; }
    }
}