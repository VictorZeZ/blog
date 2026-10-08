using blog.Domain.Common.Reports;
using FluentValidation;

namespace blog.Application.Posts.Queries.GetPostsDailyReport
{
    public class GetPostsDailyReportQueryValidator : AbstractValidator<GetPostsDailyReportQuery>
    {
        public GetPostsDailyReportQueryValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();

            this.ApplyReportDateRangeRules(x => x.From, x => x.To);
        }
    }
}