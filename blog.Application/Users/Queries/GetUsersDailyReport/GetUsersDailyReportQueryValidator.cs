using blog.Domain.Common.Reports;
using FluentValidation;

namespace blog.Application.Users.Queries.GetUsersDailyReport
{
    public class GetUsersDailyReportQueryValidator : AbstractValidator<GetUsersDailyReportQuery>
    {
        public GetUsersDailyReportQueryValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();

            RuleFor(x => x.EmailConfirmed)
                .IsInEnum();

            RuleFor(x => x.TwoFactorEnabled)
                .IsInEnum();

            this.ApplyReportDateRangeRules(x => x.From, x => x.To);
        }
    }
}