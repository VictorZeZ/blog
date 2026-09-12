using blog.Domain.Common.Reports;
using FluentValidation;

namespace blog.Application.Posts.Queries.GetPostsReport
{
    public class GetPostsReportQueryValidator : AbstractValidator<GetPostsReportQuery>
    {
        public GetPostsReportQueryValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();

            RuleFor(x => x.Paging.Page)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.Paging.PageSize)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.Filter)
                .IsInEnum();

            RuleFor(x => x.SortBy)
                .IsInEnum();

            this.ApplyReportDateRangeRules(x => x.From, x => x.To);
        }
    }
}