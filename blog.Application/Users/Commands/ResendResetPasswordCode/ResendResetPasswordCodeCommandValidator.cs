using FluentValidation;

namespace blog.Application.Users.Commands.ResendResetPasswordCode
{
    public class ResendResetPasswordCodeCommandValidator : AbstractValidator<ResendResetPasswordCodeCommand>
    {
        public ResendResetPasswordCodeCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();
        }
    }
}
