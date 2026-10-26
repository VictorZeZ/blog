using blog.Domain.Common;
using FluentValidation;

namespace blog.Application.Posts.Queries.GetAllPosts
{
    public class GetAllPostsQueryValidator : AbstractValidator<GetAllPostsQuery>
    {
        public GetAllPostsQueryValidator()
        {
            RuleFor(x => x.ActorId).NotEmpty();
            RuleFor(x => x.Paging).SetValidator(new PagedRequestValidator());
            RuleFor(x => x.SortBy).IsInEnum();
            RuleFor(x => x.Filter).IsInEnum();
        }
    }
}