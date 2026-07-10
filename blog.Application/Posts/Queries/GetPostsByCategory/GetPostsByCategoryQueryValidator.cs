using FluentValidation;

namespace blog.Application.Posts.Queries.GetPostsByCategory
{
    public class GetPostsByCategoryQueryValidator : AbstractValidator<GetPostsByCategoryQuery>
    {
        public GetPostsByCategoryQueryValidator()
        {
            RuleFor(x => x.CategorySlug).NotEmpty();
            RuleFor(x => x.Paging.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.Paging.PageSize).GreaterThanOrEqualTo(1);
            RuleFor(x => x.SortBy).IsInEnum();
        }
    }
}
