using FluentValidation;

namespace blog.Domain.Common.Reports
{
    public static class TopNRules
    {
        public const int MaxTopN = 20;
        public const int DefaultTopN = 5;

        public static IRuleBuilderOptions<T, int> ApplyTopNRules<T>(this IRuleBuilder<T, int> ruleBuilder)
            => ruleBuilder
                .GreaterThanOrEqualTo(1)
                .LessThanOrEqualTo(MaxTopN)
                    .WithMessage($"TopN must not exceed {MaxTopN}");
    }
}