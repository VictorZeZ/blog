using blog.Domain.Posts.Enums;
using FluentValidation;

namespace blog.Domain.Posts.Common
{
    public static class PostStatusFilterValidationRules
    {
        private static readonly int MaxPostStatuses = Enum.GetValues<PostStatus>().Length;

        public static IRuleBuilderOptions<T, List<PostStatus>> ApplyPostStatusListRules<T>(this IRuleBuilder<T, List<PostStatus>> ruleBuilder)
            => ruleBuilder
                .NotNull()
                .Must(statuses => statuses is null || statuses.Count <= MaxPostStatuses)
                    .WithMessage($"No more than {MaxPostStatuses} post statuses can be provided.");
    }
}