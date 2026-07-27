using FluentValidation;

namespace blog.Application.Users.Commands.ResendRegistrationCode
{
    public class ResendRegistrationCodeCommandValidator : AbstractValidator<ResendRegistrationCodeCommand>
    {
        public ResendRegistrationCodeCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();
        }
    }
}
