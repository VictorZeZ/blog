using blog.Domain.Common.Reports;
using FluentValidation;

namespace blog.Application.Posts.Queries.GetMyPostStatusReport
{
    public class GetMyPostStatusReportQueryValidator : AbstractValidator<GetMyPostStatusReportQuery>
    {
        public GetMyPostStatusReportQueryValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();

            this.ApplyReportDateRangeRules(x => x.From, x => x.To);
        }
    }
}