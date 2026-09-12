using blog.Domain.Common.Reports;
using FluentValidation;

namespace blog.Application.Categories.Queries.GetCategoryPerformanceReport
{
    public class GetCategoryPerformanceReportQueryValidator : AbstractValidator<GetCategoryPerformanceReportQuery>
    {
        public GetCategoryPerformanceReportQueryValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();

            this.ApplyReportDateRangeRules(x => x.From, x => x.To);
        }
    }
}