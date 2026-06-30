using FluentValidation;

namespace blog.Application.Posts.Commands.ChangePostStatus
{
    public class ChangePostStatusCommandValidator : AbstractValidator<ChangePostStatusCommand>
    {
        public ChangePostStatusCommandValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();

            RuleFor(x => x.PostId)
                .NotEmpty();

            RuleFor(x => x.Action)
                .IsInEnum();
        }
    }
}
