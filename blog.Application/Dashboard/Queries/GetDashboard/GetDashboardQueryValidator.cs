using FluentValidation;

namespace blog.Application.Dashboard.Queries.GetDashboard
{
    public class GetDashboardQueryValidator : AbstractValidator<GetDashboardQuery>
    {
        public GetDashboardQueryValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();
        }
    }
}
