using FluentValidation;

namespace blog.Application.Users.Commands.ResendLoginVerificationCode
{
    public class ResendLoginVerificationCodeCommandValidator : AbstractValidator<ResendLoginVerificationCodeCommand>
    {
        public ResendLoginVerificationCodeCommandValidator()
        {
            RuleFor(x => x.ChallengeId)
                .NotEmpty();
        }
    }
}
