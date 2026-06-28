using FluentValidation;

namespace blog.Application.Users.Commands.RefreshToken
{
    public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenCommandValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty();

            RuleFor(x => x.DeviceInfo)
                .NotEmpty()
                .MaximumLength(512);
        }
    }
}
