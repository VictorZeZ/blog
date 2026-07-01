using FluentValidation;

namespace blog.Application.Posts.Queries.SearchPosts
{
    public class SearchPostsQueryValidator : AbstractValidator<SearchPostsQuery>
    {
        public SearchPostsQueryValidator()
        {
            RuleFor(x => x.Term).NotEmpty().MaximumLength(256);
            RuleFor(x => x.Paging.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.Paging.PageSize).GreaterThanOrEqualTo(1);
            RuleFor(x => x.SortBy).IsInEnum();
        }
    }
}
