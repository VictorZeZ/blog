using FluentValidation;

namespace blog.Application.Posts.Queries.GetPendingApprovalPosts
{
    public class GetPendingApprovalPostsQueryValidator : AbstractValidator<GetPendingApprovalPostsQuery>
    {
        public GetPendingApprovalPostsQueryValidator()
        {
            RuleFor(x => x.ActorId).NotEmpty();
            RuleFor(x => x.Paging.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.Paging.PageSize).GreaterThanOrEqualTo(1);
            RuleFor(x => x.SortBy).IsInEnum();
        }
    }
}
