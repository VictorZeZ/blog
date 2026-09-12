using FluentValidation;

namespace blog.Domain.Common.Reports
{
    public static class ReportDateRangeValidationExtensions
    {
        public static void ApplyReportDateRangeRules<T>(this AbstractValidator<T> validator, Func<T, DateOnly?> fromSelector, Func<T, DateOnly?> toSelector)
        {
            validator.RuleFor(x => x)
                .Must(x => fromSelector(x) is null == toSelector(x) is null)
                    .WithMessage("From and To must both be provided or both be omitted");

            validator.RuleFor(x => x)
                .Must(x => fromSelector(x) is null || toSelector(x) is null || toSelector(x) >= fromSelector(x))
                    .WithMessage("To must be on or after From")
                    .When(x => fromSelector(x) is not null && toSelector(x) is not null);

            validator.RuleFor(x => x)
                .Must(x => fromSelector(x) is null || toSelector(x) is null || ReportDateRangeRules.IsWithinAllowedRange(fromSelector(x)!.Value, toSelector(x)!.Value))
                    .WithMessage($"Date range must not exceed {ReportDateRangeRules.MaxRangeDays} days")
                    .When(x => fromSelector(x) is not null && toSelector(x) is not null);
        }
    }
}