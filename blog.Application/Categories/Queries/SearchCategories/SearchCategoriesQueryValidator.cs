using blog.Domain.Common;
using blog.Domain.Posts.Common;
using FluentValidation;

namespace blog.Application.Categories.Queries.SearchCategories
{
    public class SearchCategoriesQueryValidator : AbstractValidator<SearchCategoriesQuery>
    {
        public SearchCategoriesQueryValidator()
        {
            RuleFor(x => x.Term)
                .NotEmpty()
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