using FluentValidation;

namespace blog.Application.Dashboard.Queries.GetDashboardOverview
{
    public class GetDashboardOverviewQueryValidator : AbstractValidator<GetDashboardOverviewQuery>
    {
        public GetDashboardOverviewQueryValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();
        }
    }
}