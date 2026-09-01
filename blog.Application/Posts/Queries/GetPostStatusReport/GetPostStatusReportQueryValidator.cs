using FluentValidation;

namespace blog.Application.Posts.Queries.GetPostStatusReport
{
    public class GetPostStatusReportQueryValidator : AbstractValidator<GetPostStatusReportQuery>
    {
        private const int MinRangeDays = 1;
        private const int MaxRangeDays = 30;

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
                .Must(HaveValidRangeLength)
                    .WithMessage($"Date range must span between {MinRangeDays} and {MaxRangeDays} days (inclusive)")
                    .When(x => x.To >= x.From);
        }

        private static bool HaveValidRangeLength(GetPostStatusReportQuery query)
        {
            var rangeDays = query.To.DayNumber - query.From.DayNumber + 1;
            return rangeDays is >= MinRangeDays and <= MaxRangeDays;
        }
    }
}