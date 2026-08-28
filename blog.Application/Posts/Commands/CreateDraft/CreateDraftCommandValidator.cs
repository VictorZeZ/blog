using blog.Domain.Posts.Common;
using FluentValidation;

namespace blog.Application.Posts.Commands.CreateDraft
{
    public class CreateDraftCommandValidator : AbstractValidator<CreateDraftCommand>
    {
        public CreateDraftCommandValidator()
        {
            RuleFor(x => x.AuthorId)
                .NotEmpty();

            RuleFor(x => x.CategoryId)
                .NotEmpty();

            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(256);

            RuleFor(x => x.Summary)
                .NotEmpty()
                .MaximumLength(300);

            RuleFor(x => x.Content)
                .NotEmpty();

            RuleFor(x => x.Tags)
                .NotNull();

            RuleForEach(x => x.Tags)
                .ApplyTagRules();

            RuleFor(x => x.TitleImageFileName)
                .NotEmpty()
                .When(x => x.TitleImageStream is not null)
                .WithMessage("TitleImageFileName is required when TitleImageStream is provided");
        }
    }
}