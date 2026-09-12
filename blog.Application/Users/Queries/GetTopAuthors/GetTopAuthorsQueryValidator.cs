using blog.Domain.Common.Reports;
using FluentValidation;

namespace blog.Application.Users.Queries.GetTopAuthors
{
    public class GetTopAuthorsQueryValidator : AbstractValidator<GetTopAuthorsQuery>
    {
        public GetTopAuthorsQueryValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();

            RuleFor(x => x.TopN)
                .ApplyTopNRules();

            this.ApplyReportDateRangeRules(x => x.From, x => x.To);
        }
    }
}