using FluentValidation;

namespace blog.Application.Users.Commands.ConfirmNewEmail
{
    public class ConfirmNewEmailCommandValidator : AbstractValidator<ConfirmNewEmailCommand>
    {
        public ConfirmNewEmailCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty();

            RuleFor(x => x.Code)
                .NotEmpty();
        }
    }
}
