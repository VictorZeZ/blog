using blog.Domain.Common.Interfaces;
using MediatR;

namespace blog.Application.Users.Commands.TwoFactor
{
    public class TwoFactorCommand : IRequest<TwoFactorResponse>, IRequirePasswordConfirmation
    {
        public Guid UserId { get; init; }
        public bool TwoFactor { get; init; }
        public string CurrentPassword { get; init; } = string.Empty;

        public bool RequiresPasswordConfirmation => !TwoFactor;
    }
}