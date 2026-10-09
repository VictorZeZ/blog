using blog.Domain.Categories.Repository;
using blog.Domain.Common.Reports;
using MediatR;

namespace blog.Application.Categories.Queries.GetCategoriesDailyReport
{
    public class GetCategoriesDailyReportQueryHandler(ICategoryRepository categoryRepository) : IRequestHandler<GetCategoriesDailyReportQuery, CategoriesDailyReportResponse>
    {
        public async Task<CategoriesDailyReportResponse> Handle(GetCategoriesDailyReportQuery request, CancellationToken cancellationToken)
        {
            var range = ReportDateRangeRules.Resolve(request.From, request.To);

            var days = await categoryRepository.GetDailyActivityReportAsync(range.From, range.To, cancellationToken);

            return new CategoriesDailyReportResponse
            {
                From = range.From,
                To = range.To,
                CreatedCount = days.Sum(d => d.CreatedCount),
                DeletedCount = days.Sum(d => d.DeletedCount),
                Days = days.Select(d => new CategoryDailyCountResponse
                {
                    Date = d.Date,
                    CreatedCount = d.CreatedCount,
                    DeletedCount = d.DeletedCount
                }).ToList()
            };
        }
    }
}