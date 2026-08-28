using FluentValidation;

namespace blog.Application.Posts.Commands.PublishDraft
{
    public class PublishDraftCommandValidator : AbstractValidator<PublishDraftCommand>
    {
        public PublishDraftCommandValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();

            RuleFor(x => x.PostId)
                .NotEmpty();
        }
    }
}