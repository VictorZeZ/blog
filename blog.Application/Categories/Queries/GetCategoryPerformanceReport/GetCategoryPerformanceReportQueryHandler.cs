using blog.Domain.Common.Reports;
using blog.Domain.Posts.Repository;
using MediatR;

namespace blog.Application.Categories.Queries.GetCategoryPerformanceReport
{
    public class GetCategoryPerformanceReportQueryHandler(IPostRepository postRepository) : IRequestHandler<GetCategoryPerformanceReportQuery, GetCategoryPerformanceReportResponse>
    {
        public async Task<GetCategoryPerformanceReportResponse> Handle(GetCategoryPerformanceReportQuery request, CancellationToken cancellationToken)
        {
            var range = ReportDateRangeRules.Resolve(request.From, request.To);

            var breakdown = await postRepository.GetCategoryBreakdownAsync(range.From, range.To, cancellationToken);

            return new GetCategoryPerformanceReportResponse
            {
                From = range.From,
                To = range.To,
                Categories = breakdown.Select(c => new CategoryPerformanceItemResponse
                {
                    CategoryId = c.CategoryId,
                    Name = c.Name,
                    DraftCount = c.DraftCount,
                    PendingApprovalCount = c.PendingApprovalCount,
                    PublishedCount = c.PublishedCount,
                    RejectedCount = c.RejectedCount,
                    TotalCount = c.TotalCount,
                    TotalViewCount = c.TotalViewCount
                }).ToList()
            };
        }
    }
}