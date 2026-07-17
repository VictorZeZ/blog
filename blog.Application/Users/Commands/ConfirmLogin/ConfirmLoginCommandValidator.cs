using FluentValidation;

namespace blog.Application.Users.Commands.ConfirmLogin
{
    public class ConfirmLoginCommandValidator : AbstractValidator<ConfirmLoginCommand>
    {
        public ConfirmLoginCommandValidator()
        {
            RuleFor(x => x.ChallengeId)
                .NotEmpty();

            RuleFor(x => x.Code)
                .NotEmpty();

            RuleFor(x => x.DeviceInfo)
                .NotEmpty()
                .MaximumLength(512);
        }
    }
}
