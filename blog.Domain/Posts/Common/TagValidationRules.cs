using FluentValidation;

namespace blog.Domain.Posts.Common
{
    public static class TagValidationRules
    {
        private const int MaxTagLength = 50;
        private static readonly char[] StructuralCharacters = ['[', ']', '"', ','];

        public static IRuleBuilderOptions<T, string> ApplyTagRules<T>(this IRuleBuilder<T, string> ruleBuilder)
            => ruleBuilder
                .NotEmpty()
                .MaximumLength(MaxTagLength)
                .Must(NotContainStructuralCharacters)
                    .WithMessage("Tag must not contain brackets, quotes, or commas.");

        private static bool NotContainStructuralCharacters(string tag)
            => tag.IndexOfAny(StructuralCharacters) == -1;
    }
}
