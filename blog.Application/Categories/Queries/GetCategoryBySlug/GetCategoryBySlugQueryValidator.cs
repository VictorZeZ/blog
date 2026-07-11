using FluentValidation;

namespace blog.Application.Categories.Queries.GetCategoryBySlug
{
    public class GetCategoryBySlugQueryValidator : AbstractValidator<GetCategoryBySlugQuery>
    {
        public GetCategoryBySlugQueryValidator()
        {
            RuleFor(x => x.Slug)
                .NotEmpty();
        }
    }
}
