using blog.Domain.Common;
using FluentValidation;

namespace blog.Application.Posts.Queries.GetAllPublishedPosts
{
    public class GetAllPublishedPostsQueryValidator : AbstractValidator<GetAllPublishedPostsQuery>
    {
        public GetAllPublishedPostsQueryValidator()
        {
            RuleFor(x => x.Paging).SetValidator(new PagedRequestValidator());
            RuleFor(x => x.SortBy).IsInEnum();
        }
    }
}