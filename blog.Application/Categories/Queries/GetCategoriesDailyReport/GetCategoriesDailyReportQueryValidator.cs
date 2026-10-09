using blog.Domain.Common.Reports;
using FluentValidation;

namespace blog.Application.Categories.Queries.GetCategoriesDailyReport
{
    public class GetCategoriesDailyReportQueryValidator : AbstractValidator<GetCategoriesDailyReportQuery>
    {
        public GetCategoriesDailyReportQueryValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();

            this.ApplyReportDateRangeRules(x => x.From, x => x.To);
        }
    }
}