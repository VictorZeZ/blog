using blog.Domain.Users.Common;
using FluentValidation;

namespace blog.Application.Users.Commands.ChangePassword
{
    public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty();

            RuleFor(x => x.CurrentPassword)
                .NotEmpty();

            RuleFor(x => x.NewPassword)
                .ApplyPasswordRules()
                .NotEqual(x => x.CurrentPassword).WithMessage("New password must be different from current password");
        }
    }
}
