using FluentValidation;

namespace blog.Application.Users.Commands.RevokeSession
{
    public class RevokeSessionCommandValidator : AbstractValidator<RevokeSessionCommand>
    {
        public RevokeSessionCommandValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();

            RuleFor(x => x.SessionId)
                .NotEmpty();
        }
    }
}