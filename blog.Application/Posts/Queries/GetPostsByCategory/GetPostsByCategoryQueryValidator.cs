using blog.Domain.Common;
using FluentValidation;

namespace blog.Application.Posts.Queries.GetPostsByCategory
{
    public class GetPostsByCategoryQueryValidator : AbstractValidator<GetPostsByCategoryQuery>
    {
        public GetPostsByCategoryQueryValidator()
        {
            RuleFor(x => x.CategorySlug).NotEmpty();
            RuleFor(x => x.Paging).SetValidator(new PagedRequestValidator());
            RuleFor(x => x.SortBy).IsInEnum();
        }
    }
}