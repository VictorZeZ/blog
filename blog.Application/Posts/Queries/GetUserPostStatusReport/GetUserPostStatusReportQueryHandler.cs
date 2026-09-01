using blog.Domain.Posts.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Posts.Queries.GetUserPostStatusReport
{
    public class GetUserPostStatusReportQueryHandler(IPostRepository postRepository) : IRequestHandler<GetUserPostStatusReportQuery, GetUserPostStatusReportResponse>
    {
        public async Task<GetUserPostStatusReportResponse> Handle(GetUserPostStatusReportQuery request, CancellationToken cancellationToken)
        {
            var report = await postRepository.GetStatusReportByAuthorAsync(new UserId(request.AuthorId), request.From, request.To, cancellationToken);

            return new GetUserPostStatusReportResponse
            {
                AuthorId = request.AuthorId,
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