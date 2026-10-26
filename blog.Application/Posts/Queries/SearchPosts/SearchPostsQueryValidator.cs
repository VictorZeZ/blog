using blog.Domain.Common;
using FluentValidation;

namespace blog.Application.Posts.Queries.SearchPosts
{
    public class SearchPostsQueryValidator : AbstractValidator<SearchPostsQuery>
    {
        public SearchPostsQueryValidator()
        {
            RuleFor(x => x.Term).NotEmpty().MaximumLength(256);
            RuleFor(x => x.Paging).SetValidator(new PagedRequestValidator());
            RuleFor(x => x.SortBy).IsInEnum();
        }
    }
}