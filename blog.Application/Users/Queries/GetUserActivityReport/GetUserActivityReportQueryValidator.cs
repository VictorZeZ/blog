using blog.Domain.Common.Reports;
using FluentValidation;

namespace blog.Application.Users.Queries.GetUserActivityReport
{
    public class GetUserActivityReportQueryValidator : AbstractValidator<GetUserActivityReportQuery>
    {
        public GetUserActivityReportQueryValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();

            this.ApplyReportDateRangeRules(x => x.From, x => x.To);
        }
    }
}