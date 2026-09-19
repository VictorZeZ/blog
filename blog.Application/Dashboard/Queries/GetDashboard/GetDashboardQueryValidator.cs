using blog.Domain.Common.Reports;
using FluentValidation;

namespace blog.Application.Dashboard.Queries.GetDashboard
{
    public class GetDashboardQueryValidator : AbstractValidator<GetDashboardQuery>
    {
        public GetDashboardQueryValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();

            RuleFor(x => x.Scope)
                .IsInEnum();

            this.ApplyReportDateRangeRules(x => x.From, x => x.To);
        }
    }
}