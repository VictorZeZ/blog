using FluentValidation;

namespace blog.Application.Users.Commands.DeleteAccount
{
    public class DeleteAccountCommandValidator : AbstractValidator<DeleteAccountCommand>
    {
        public DeleteAccountCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty();

            RuleFor(x => x.CurrentPassword)
                .NotEmpty();
        }
    }
}
