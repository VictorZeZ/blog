using blog.Domain.Common.Reports;
using blog.Domain.Posts.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Posts.Queries.GetMyPostStatusReport
{
    public class GetMyPostStatusReportQueryHandler(IPostRepository postRepository) : IRequestHandler<GetMyPostStatusReportQuery, GetMyPostStatusReportResponse>
    {
        public async Task<GetMyPostStatusReportResponse> Handle(GetMyPostStatusReportQuery request, CancellationToken cancellationToken)
        {
            var range = ReportDateRangeRules.Resolve(request.From, request.To);

            var report = await postRepository.GetStatusReportByAuthorAsync(new UserId(request.ActorId), range.From, range.To, cancellationToken);

            return new GetMyPostStatusReportResponse
            {
                From = report.From,
                To = report.To,
                DraftCount = report.DraftCount,
                PendingApprovalCount = report.PendingApprovalCount,
                PublishedCount = report.PublishedCount,
                RejectedCount = report.RejectedCount,
                TotalCount = report.TotalCount
            };
        }
    }
}