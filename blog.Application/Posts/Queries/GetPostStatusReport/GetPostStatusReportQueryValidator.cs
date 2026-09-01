using blog.Domain.Posts.Common;
using FluentValidation;

namespace blog.Application.Posts.Queries.GetPostStatusReport
{
    public class GetPostStatusReportQueryValidator : AbstractValidator<GetPostStatusReportQuery>
    {
        public GetPostStatusReportQueryValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();

            RuleFor(x => x.From)
                .NotEmpty();

            RuleFor(x => x.To)
                .NotEmpty()
                .GreaterThanOrEqualTo(x => x.From)
                    .WithMessage("To must be on or after From");

            RuleFor(x => x)
                .Must(x => DateRangeValidationRules.IsWithinAllowedRange(x.From, x.To))
                    .WithMessage($"Date range must span between {DateRangeValidationRules.MinRangeDays} and {DateRangeValidationRules.MaxRangeDays} days (inclusive)")
                    .When(x => x.To >= x.From);
        }
    }
}