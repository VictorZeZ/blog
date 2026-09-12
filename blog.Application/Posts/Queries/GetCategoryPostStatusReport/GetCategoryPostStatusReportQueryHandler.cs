using blog.Domain.Categories.Repository;
using blog.Domain.Categories.Types;
using blog.Domain.Common.Reports;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Repository;
using MediatR;

namespace blog.Application.Posts.Queries.GetCategoryPostStatusReport
{
    public class GetCategoryPostStatusReportQueryHandler(IPostRepository postRepository, ICategoryRepository categoryRepository) : IRequestHandler<GetCategoryPostStatusReportQuery, GetCategoryPostStatusReportResponse>
    {
        public async Task<GetCategoryPostStatusReportResponse> Handle(GetCategoryPostStatusReportQuery request, CancellationToken cancellationToken)
        {
            var categoryId = new CategoryId(request.CategoryId);
            var category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);
            if (category is null)
                throw new NotFoundException("Category", request.CategoryId);

            var range = ReportDateRangeRules.Resolve(request.From, request.To);

            var report = await postRepository.GetStatusReportByCategoryAsync(categoryId, range.From, range.To, cancellationToken);

            return new GetCategoryPostStatusReportResponse
            {
                CategoryId = request.CategoryId,
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