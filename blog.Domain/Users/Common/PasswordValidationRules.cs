using FluentValidation;

namespace blog.Domain.Users.Common
{
    public static class PasswordValidationRules
    {
        public static IRuleBuilderOptions<T, string> ApplyPasswordRules<T>(this IRuleBuilder<T, string> ruleBuilder)
            => ruleBuilder
                .NotEmpty()
                .MinimumLength(8)
                .MaximumLength(128)
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
