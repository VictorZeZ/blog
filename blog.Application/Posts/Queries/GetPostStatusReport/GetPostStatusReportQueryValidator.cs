using blog.Domain.Common.Reports;
using FluentValidation;

namespace blog.Application.Posts.Queries.GetPostStatusReport
{
    public class GetPostStatusReportQueryValidator : AbstractValidator<GetPostStatusReportQuery>
    {
        public GetPostStatusReportQueryValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();

            this.ApplyReportDateRangeRules(x => x.From, x => x.To);
        }
    }
}