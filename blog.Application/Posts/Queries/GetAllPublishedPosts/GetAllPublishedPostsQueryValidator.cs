using FluentValidation;

namespace blog.Application.Posts.Queries.GetAllPublishedPosts
{
    public class GetAllPublishedPostsQueryValidator : AbstractValidator<GetAllPublishedPostsQuery>
    {
        public GetAllPublishedPostsQueryValidator()
        {
            RuleFor(x => x.Paging.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.Paging.PageSize).GreaterThanOrEqualTo(1);
            RuleFor(x => x.SortBy).IsInEnum();
        }
    }
}
