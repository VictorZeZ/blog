using FluentValidation;

namespace blog.Application.Users.Commands.ConfirmChangeEmail
{
    public class ConfirmChangeEmailCommandValidator : AbstractValidator<ConfirmChangeEmailCommand>
    {
        public ConfirmChangeEmailCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty();

            RuleFor(x => x.Code)
                .NotEmpty();
        }
    }
}
