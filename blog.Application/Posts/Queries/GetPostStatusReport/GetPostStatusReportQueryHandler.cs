using blog.Domain.Posts.Repository;
using MediatR;

namespace blog.Application.Posts.Queries.GetPostStatusReport
{
    public class GetPostStatusReportQueryHandler(IPostRepository postRepository) : IRequestHandler<GetPostStatusReportQuery, GetPostStatusReportResponse>
    {
        public async Task<GetPostStatusReportResponse> Handle(GetPostStatusReportQuery request, CancellationToken cancellationToken)
        {
            var report = await postRepository.GetStatusReportAsync(request.From, request.To, cancellationToken);

            return new GetPostStatusReportResponse
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