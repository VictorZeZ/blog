using blog.Domain.Common;
using blog.Domain.Posts.Common;
using FluentValidation;

namespace blog.Application.Categories.Queries.GetCategories
{
    public class GetCategoriesQueryValidator : AbstractValidator<GetCategoriesQuery>
    {
        public GetCategoriesQueryValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();

            RuleFor(x => x.Term)
                .MaximumLength(256);

            RuleFor(x => x.Paging)
                .SetValidator(new PagedRequestValidator());

            RuleFor(x => x.SortBy)
                .IsInEnum();

            RuleFor(x => x.Deleted)
                .IsInEnum();

            RuleFor(x => x.PostStatuses)
                .ApplyPostStatusListRules();

            RuleForEach(x => x.PostStatuses)
                .IsInEnum();
        }
    }
}