using FluentValidation;

namespace blog.Application.Users.Commands.TwoFactor
{
    public class TwoFactorCommandValidator : AbstractValidator<TwoFactorCommand>
    {
        public TwoFactorCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty();
        }
    }
}
