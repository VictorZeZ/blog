using FluentValidation;

namespace blog.Application.Posts.Commands.UpdatePost
{
    public class UpdatePostCommandValidator : AbstractValidator<UpdatePostCommand>
    {
        public UpdatePostCommandValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();

            RuleFor(x => x.PostId)
                .NotEmpty();

            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(256);

            RuleFor(x => x.Content)
                .NotEmpty();

            RuleFor(x => x.Tags)
                .NotNull();

            RuleForEach(x => x.Tags)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(x => x.TitleImageFileName)
                .NotEmpty()
                .When(x => x.TitleImageStream is not null)
                .WithMessage("TitleImageFileName is required when TitleImageStream is provided");

            RuleFor(x => x.RemoveTitleImage)
                .Equal(false)
                .When(x => x.TitleImageStream is not null)
                .WithMessage("Cannot remove and replace TitleImage in the same request");
        }
    }
}
