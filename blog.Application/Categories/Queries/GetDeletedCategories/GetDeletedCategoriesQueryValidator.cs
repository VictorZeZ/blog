using FluentValidation;

namespace blog.Application.Categories.Queries.GetDeletedCategories
{
    public class GetDeletedCategoriesQueryValidator : AbstractValidator<GetDeletedCategoriesQuery>
    {
        public GetDeletedCategoriesQueryValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();
        }
    }
}