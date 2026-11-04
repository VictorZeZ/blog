using blog.Domain.Common.Interfaces;
using blog.Domain.Common.Settings;
using blog.Domain.Exceptions;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;
using Microsoft.Extensions.Options;

namespace blog.Application.Common
{
    public class PasswordConfirmationBehavior<TRequest, TResponse>(IUserRepository userRepository, IPasswordHasher passwordHasher, IOptions<AccountLockoutSettings> lockoutSettings, IUnitOfWork unitOfWork) : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (request is not IRequirePasswordConfirmation confirmation || !confirmation.RequiresPasswordConfirmation)
                return await next();

            var user = await userRepository.GetByIdAsync(new UserId(confirmation.UserId), cancellationToken);
            if (user is null)
                throw new NotFoundException("User", confirmation.UserId);

            user.EnsureActive();
            user.EnsureNotLockedOut();

            var settings = lockoutSettings.Value;
            var lockedUntil = await userRepository.RegisterPasswordAttemptAsync(user.Id, settings.MaxFailedAttempts, TimeSpan.FromMinutes(settings.LockoutDurationMinutes), cancellationToken);
            if (lockedUntil is not null)
                throw new LockedException("User", lockedUntil.Value);

            if (!passwordHasher.Verify(confirmation.CurrentPassword, user.PasswordHash))
                throw new ValidationException("CurrentPassword", "Current password is incorrect");

            user.ResetFailedLoginAttempts();
            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return await next();
        }
    }
}