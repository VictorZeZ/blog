using FluentValidation;

namespace blog.Application.Users.Queries.SearchUsers
{
    public class SearchUsersQueryValidator : AbstractValidator<SearchUsersQuery>
    {
        public SearchUsersQueryValidator()
        {
            RuleFor(x => x.Term)
                .NotEmpty()
                .MaximumLength(256);

            RuleFor(x => x.Paging.Page)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.Paging.PageSize)
                .GreaterThanOrEqualTo(1);
        }
    }
}
