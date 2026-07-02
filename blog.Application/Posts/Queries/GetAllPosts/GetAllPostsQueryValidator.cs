using FluentValidation;

namespace blog.Application.Posts.Queries.GetAllPosts
{
    public class GetAllPostsQueryValidator : AbstractValidator<GetAllPostsQuery>
    {
        public GetAllPostsQueryValidator()
        {
            RuleFor(x => x.ActorId).NotEmpty();
            RuleFor(x => x.Paging.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.Paging.PageSize).GreaterThanOrEqualTo(1);
            RuleFor(x => x.SortBy).IsInEnum();
            RuleFor(x => x.Filter).IsInEnum();
        }
    }
}
