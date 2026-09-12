using blog.Domain.Common.Reports;
using FluentValidation;

namespace blog.Application.Posts.Queries.GetTopPosts
{
    public class GetTopPostsQueryValidator : AbstractValidator<GetTopPostsQuery>
    {
        public GetTopPostsQueryValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();

            RuleFor(x => x.TopN)
                .ApplyTopNRules();

            this.ApplyReportDateRangeRules(x => x.From, x => x.To);
        }
    }
}