using FluentValidation;

namespace blog.Domain.Posts.Common
{
    public static class TagValidationRules
    {
        private const int MaxTagLength = 50;
        private const int MaxTagsPerPost = 20;
        private static readonly char[] StructuralCharacters = ['[', ']', '"', ','];

        public static IRuleBuilderOptions<T, string> ApplyTagRules<T>(this IRuleBuilder<T, string> ruleBuilder)
            => ruleBuilder
                .NotEmpty()
                .MaximumLength(MaxTagLength)
                .Must(NotContainStructuralCharacters)
                    .WithMessage("Tag must not contain brackets, quotes, or commas.");

        public static IRuleBuilderOptions<T, List<string>> ApplyTagListRules<T>(this IRuleBuilder<T, List<string>> ruleBuilder)
            => ruleBuilder
                .NotNull()
                .Must(tags => tags is null || tags.Count <= MaxTagsPerPost)
                    .WithMessage($"No more than {MaxTagsPerPost} tags are allowed per post.");

        private static bool NotContainStructuralCharacters(string tag)
            => tag.IndexOfAny(StructuralCharacters) == -1;
    }
}