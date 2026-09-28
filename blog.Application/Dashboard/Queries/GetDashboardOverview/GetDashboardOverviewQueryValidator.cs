using FluentValidation;

namespace blog.Application.Dashboard.Queries.GetDashboardOverview
{
    public class GetDashboardOverviewQueryValidator : AbstractValidator<GetDashboardOverviewQuery>
    {
        public GetDashboardOverviewQueryValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();

            RuleFor(x => x.PostFilter)
                .IsInEnum();

            RuleFor(x => x.UserLevelFilter)
                .IsInEnum();

            RuleFor(x => x.UserBanned)
                .IsInEnum();

            RuleFor(x => x.UserDeleted)
                .IsInEnum();

            RuleFor(x => x.UserEmailConfirmed)
                .IsInEnum();

            RuleFor(x => x.UserTwoFactorEnabled)
                .IsInEnum();

            RuleFor(x => x.CategoryDeleted)
                .IsInEnum();
        }
    }
}