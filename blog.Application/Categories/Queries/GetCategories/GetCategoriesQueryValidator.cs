using blog.Domain.Common;
using blog.Domain.Posts.Enums;
using FluentValidation;

namespace blog.Application.Categories.Queries.GetCategories
{
    public class GetCategoriesQueryValidator : AbstractValidator<GetCategoriesQuery>
    {
        private static readonly int MaxPostStatuses = Enum.GetValues<PostStatus>().Length;

        public GetCategoriesQueryValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();

            RuleFor(x => x.Paging)
                .SetValidator(new PagedRequestValidator());

            RuleFor(x => x.SortBy)
                .IsInEnum();

            RuleFor(x => x.Deleted)
                .IsInEnum();

            RuleFor(x => x.PostStatuses)
                .NotNull()
                .Must(statuses => statuses is null || statuses.Count <= MaxPostStatuses)
                    .WithMessage($"No more than {MaxPostStatuses} post statuses can be provided.");

            RuleForEach(x => x.PostStatuses)
                .IsInEnum();
        }
    }
}