using FluentValidation;

namespace blog.Application.Users.Commands.BanUser
{
    public class BanUserCommandValidator : AbstractValidator<BanUserCommand>
    {
        public BanUserCommandValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();

            RuleFor(x => x.TargetUserId)
                .NotEmpty();

            RuleFor(x => x.TargetUserId)
                .NotEmpty()
                .NotEqual(x => x.ActorId)
                .WithMessage("Actor and target cannot be the same user");
        }
    }
}
