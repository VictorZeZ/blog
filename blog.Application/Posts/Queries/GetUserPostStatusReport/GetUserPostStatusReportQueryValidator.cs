using blog.Domain.Common.Reports;
using FluentValidation;

namespace blog.Application.Posts.Queries.GetUserPostStatusReport
{
    public class GetUserPostStatusReportQueryValidator : AbstractValidator<GetUserPostStatusReportQuery>
    {
        public GetUserPostStatusReportQueryValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();

            RuleFor(x => x.AuthorId)
                .NotEmpty();

            this.ApplyReportDateRangeRules(x => x.From, x => x.To);
        }
    }
}