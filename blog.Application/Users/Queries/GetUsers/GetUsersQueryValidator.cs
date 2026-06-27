using FluentValidation;

namespace blog.Application.Users.Queries.GetUsers
{
    public class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
    {
        public GetUsersQueryValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();

            RuleFor(x => x.Paging.Page)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.Paging.PageSize)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.SortBy)
                .IsInEnum();

            RuleFor(x => x.Filter)
                .IsInEnum();
        }
    }
}
