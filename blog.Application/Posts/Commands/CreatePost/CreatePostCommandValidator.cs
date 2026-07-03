using blog.Application.Posts.Commands.UpdatePost;
using blog.Domain.Posts.Common;
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
                .ApplyTagRules();

            RuleFor(x => x.RemoveTitleImage)
                .Equal(false)
                .When(x => x.TitleImageStream is not null)
                .WithMessage("Cannot remove and replace TitleImage in the same request");
        }
    }
}
