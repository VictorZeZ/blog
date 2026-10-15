using FluentValidation;

namespace blog.Domain.Common.Helpers
{
    public static class SlugValidationRules
    {
        public static IRuleBuilderOptions<T, string> ApplySlugSourceRules<T>(this IRuleBuilder<T, string> ruleBuilder)
            => ruleBuilder
                .Must(value => string.IsNullOrWhiteSpace(value) || SlugGenerator.Generate(value).Length > 0)
                    .WithMessage("Value must contain at least one letter or digit.");
    }
}