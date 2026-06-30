using FluentValidation;

namespace blog.Application.Posts.Commands.CreatePost
{
    public class CreatePostCommandValidator : AbstractValidator<CreatePostCommand>
    {
        public CreatePostCommandValidator()
        {
            RuleFor(x => x.AuthorId)
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
        }
    }
}
