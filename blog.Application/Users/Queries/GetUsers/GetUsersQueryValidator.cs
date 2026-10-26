using blog.Domain.Common;
using FluentValidation;

namespace blog.Application.Users.Queries.GetUsers
{
    public class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
    {
        public GetUsersQueryValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();

            RuleFor(x => x.Paging)
                .SetValidator(new PagedRequestValidator());

            RuleFor(x => x.SortBy)
                .IsInEnum();

            RuleFor(x => x.Filter)
                .IsInEnum();

            RuleFor(x => x.Banned)
                .IsInEnum();

            RuleFor(x => x.Deleted)
                .IsInEnum();

            RuleFor(x => x.EmailConfirmed)
                .IsInEnum();

            RuleFor(x => x.TwoFactorEnabled)
                .IsInEnum();
        }
    }
}