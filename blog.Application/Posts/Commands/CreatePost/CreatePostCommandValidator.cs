using blog.Application.Posts.Commands.UpdatePost;
using FluentValidation;

namespace blog.Application.Posts.Commands.CreatePost
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
                .MaximumLength(50)
                .Must(NotContainStructuralCharacters)
                    .WithMessage("Tag must not contain brackets, quotes, or commas.");

            RuleFor(x => x.RemoveTitleImage)
                .Equal(false)
                .When(x => x.TitleImageStream is not null)
                .WithMessage("Cannot remove and replace TitleImage in the same request");
        }

        private static bool NotContainStructuralCharacters(string tag)
            => tag.IndexOfAny(['[', ']', '"', ',']) == -1;
    }
}
