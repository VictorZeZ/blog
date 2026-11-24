using blog.Domain.Common;
using FluentValidation;

namespace blog.Application.Posts.Queries.GetDashboardPosts
{
    public class GetDashboardPostsQueryValidator : AbstractValidator<GetDashboardPostsQuery>
    {
        public GetDashboardPostsQueryValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();

            RuleFor(x => x.Term)
                .MaximumLength(256);

            RuleFor(x => x.Paging)
                .SetValidator(new PagedRequestValidator());

            RuleFor(x => x.SortBy)
                .IsInEnum();

            RuleFor(x => x.Filter)
                .IsInEnum();
        }
    }
}