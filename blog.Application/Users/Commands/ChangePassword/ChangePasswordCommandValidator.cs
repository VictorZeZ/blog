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
                .NotEmpty()
                .MinimumLength(8)
                .MaximumLength(128)
                .NotEqual(x => x.CurrentPassword).WithMessage("New password must be different from current password")
                .Matches("[A-Z]")
                    .WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[a-z]")
                    .WithMessage("Password must contain at least one lowercase letter.")
                .Matches("[0-9]")
                    .WithMessage("Password must contain at least one number.")
                .Matches(@"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>/?]")
                    .WithMessage("Password must contain at least one special character.")
                .Must(p => !p.Contains(' '))
                    .WithMessage("Password cannot contain spaces.");
        }
    }
}
