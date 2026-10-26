using FluentValidation;

namespace blog.Domain.Posts.Common
{
    public static class PostContentValidationRules
    {
        private const int MaxContentLength = 200_000;

        public static IRuleBuilderOptions<T, string> ApplyContentRules<T>(this IRuleBuilder<T, string> ruleBuilder)
            => ruleBuilder
                .NotEmpty()
                .MaximumLength(MaxContentLength);
    }
}