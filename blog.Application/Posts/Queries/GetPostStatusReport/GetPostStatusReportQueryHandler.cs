using blog.Domain.Common.Reports;
using blog.Domain.Posts.Repository;
using MediatR;

namespace blog.Application.Posts.Queries.GetPostStatusReport
{
    public class GetPostStatusReportQueryHandler(IPostRepository postRepository) : IRequestHandler<GetPostStatusReportQuery, GetPostStatusReportResponse>
    {
        public async Task<GetPostStatusReportResponse> Handle(GetPostStatusReportQuery request, CancellationToken cancellationToken)
        {
            var range = ReportDateRangeRules.Resolve(request.From, request.To);

            var report = await postRepository.GetStatusReportAsync(range.From, range.To, cancellationToken);

            return new GetPostStatusReportResponse
            {
                From = report.From,
                To = report.To,
                DraftCount = report.DraftCount,
                PendingApprovalCount = report.PendingApprovalCount,
                PublishedCount = report.PublishedCount,
                RejectedCount = report.RejectedCount,
                TotalCount = report.TotalCount,
                DailyBreakdown = report.DailyBreakdown
            };
        }
    }
}