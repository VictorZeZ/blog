using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;

namespace blog.Application.Users.Commands.TwoFactor
{
    public class TwoFactorCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork) : IRequestHandler<TwoFactorCommand, TwoFactorResponse>
    {
        public async Task<TwoFactorResponse> Handle(TwoFactorCommand request, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByIdAsync(new UserId(request.UserId), cancellationToken);
            if (user is null)
                throw new NotFoundException("User", request.UserId);

            user.EnsureActive();
            user.EnsureEmailConfirmed();

            if (!request.TwoFactor && !passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
                throw new ValidationException("CurrentPassword", "Current password is incorrect");

            if (user.TwoFactorEnabled == request.TwoFactor)
                throw new InvalidStateException(
                    "User",
                    request.TwoFactor ? "TwoFactorEnabled" : "TwoFactorDisabled",
                    request.TwoFactor ? "TwoFactorDisabled" : "TwoFactorEnabled");

            if (request.TwoFactor)
                user.EnableTwoFactor();
            else
                user.DisableTwoFactor();

            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new TwoFactorResponse
            {
                Id = user.Id.Value,
                TwoFactorEnabled = user.TwoFactorEnabled
            };
        }
    }
}