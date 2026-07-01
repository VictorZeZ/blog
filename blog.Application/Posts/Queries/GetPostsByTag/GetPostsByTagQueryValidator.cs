using FluentValidation;

namespace blog.Application.Posts.Queries.GetPostsByTag
{
    public class GetPostsByTagQueryValidator : AbstractValidator<GetPostsByTagQuery>
    {
        public GetPostsByTagQueryValidator()
        {
            RuleFor(x => x.Tag).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Paging.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.Paging.PageSize).GreaterThanOrEqualTo(1);
            RuleFor(x => x.SortBy).IsInEnum();
        }
    }
}
