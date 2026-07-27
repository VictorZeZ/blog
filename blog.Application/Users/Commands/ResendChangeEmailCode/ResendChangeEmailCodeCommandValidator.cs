using FluentValidation;

namespace blog.Application.Users.Commands.ResendChangeEmailCode
{
    public class ResendChangeEmailCodeCommandValidator : AbstractValidator<ResendChangeEmailCodeCommand>
    {
        public ResendChangeEmailCodeCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty();
        }
    }
}
