using blog.Domain.Common.Reports;
using FluentValidation;

namespace blog.Application.Posts.Queries.GetCategoryPostStatusReport
{
    public class GetCategoryPostStatusReportQueryValidator : AbstractValidator<GetCategoryPostStatusReportQuery>
    {
        public GetCategoryPostStatusReportQueryValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();

            RuleFor(x => x.CategoryId)
                .NotEmpty();

            this.ApplyReportDateRangeRules(x => x.From, x => x.To);
        }
    }
}