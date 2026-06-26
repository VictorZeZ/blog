using blog.Domain.Users.Enums;
using FluentValidation;

namespace blog.Application.Users.Commands.ChangeUserLevel
{
    public class ChangeUserLevelCommandValidator : AbstractValidator<ChangeUserLevelCommand>
    {
        public ChangeUserLevelCommandValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();

            RuleFor(x => x.TargetUserId)
                .NotEmpty();

            RuleFor(x => x.Level)
                .IsInEnum()
                .NotEqual(UserLevel.Owner)
                .WithMessage("Cannot set user level to Owner");
        }
    }
}
