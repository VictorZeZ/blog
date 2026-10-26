using blog.Domain.Common;
using FluentValidation;

namespace blog.Application.Posts.Queries.GetPendingApprovalPosts
{
    public class GetPendingApprovalPostsQueryValidator : AbstractValidator<GetPendingApprovalPostsQuery>
    {
        public GetPendingApprovalPostsQueryValidator()
        {
            RuleFor(x => x.ActorId).NotEmpty();
            RuleFor(x => x.Paging).SetValidator(new PagedRequestValidator());
            RuleFor(x => x.SortBy).IsInEnum();
        }
    }
}