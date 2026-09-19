using blog.Domain.Common.Reports;
using blog.Domain.Posts.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Posts.Queries.GetUserPostStatusReport
{
    public class GetUserPostStatusReportQueryHandler(IPostRepository postRepository) : IRequestHandler<GetUserPostStatusReportQuery, GetUserPostStatusReportResponse>
    {
        public async Task<GetUserPostStatusReportResponse> Handle(GetUserPostStatusReportQuery request, CancellationToken cancellationToken)
        {
            var range = ReportDateRangeRules.Resolve(request.From, request.To);

            var report = await postRepository.GetStatusReportByAuthorAsync(new UserId(request.AuthorId), range.From, range.To, cancellationToken);

            return new GetUserPostStatusReportResponse
            {
                AuthorId = request.AuthorId,
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