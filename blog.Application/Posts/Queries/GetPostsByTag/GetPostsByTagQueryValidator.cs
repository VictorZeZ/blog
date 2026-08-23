using blog.Domain.Posts.Common;
using FluentValidation;

namespace blog.Application.Posts.Queries.GetPostsByTag
{
    public class GetPostsByTagQueryValidator : AbstractValidator<GetPostsByTagQuery>
    {
        private const int MaxTagsPerQuery = 10;

        public GetPostsByTagQueryValidator()
        {
            RuleFor(x => x.Tags)
                .NotEmpty()
                .Must(tags => tags.Count <= MaxTagsPerQuery)
                    .WithMessage($"No more than {MaxTagsPerQuery} tags can be searched at once.");

            RuleForEach(x => x.Tags)
                .ApplyTagRules();

            RuleFor(x => x.Paging.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.Paging.PageSize).GreaterThanOrEqualTo(1);
            RuleFor(x => x.SortBy).IsInEnum();
            RuleFor(x => x.GroupingMode).IsInEnum();
        }
    }
}
