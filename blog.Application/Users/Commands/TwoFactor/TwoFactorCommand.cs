using MediatR;

namespace blog.Application.Users.Commands.TwoFactor
{
    public class TwoFactorCommand : IRequest<TwoFactorResponse>
    {
        public Guid UserId { get; init; }
        public bool TwoFactor { get; init; }
    }
}
