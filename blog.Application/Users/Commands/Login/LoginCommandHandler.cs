using blog.Domain.Common.Interfaces;
using blog.Domain.Common.Settings;
using blog.Domain.Exceptions;
using blog.Domain.Tokens.Repository;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using MediatR;
using Microsoft.Extensions.Options;
using RefreshTokenEntity = blog.Domain.Tokens.Entities.RefreshToken;

namespace blog.Application.Users.Commands.Login
{
    public class LoginCommandHandler(IUserRepository userRepository, IRefreshTokenRepository refreshTokenRepository, IPasswordHasher passwordHasher, IHasher tokenHasher, IJwtService jwtService, IOptions<AccountLockoutSettings> lockoutSettings, IUnitOfWork unitOfWork) : IRequestHandler<LoginCommand, LoginResponse>
    {
        public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

            if (user is not null && user.IsLockedOut())
                throw new LockedException("User", user.LockedOutUntil!.Value);

            var isPasswordValid = user is not null && passwordHasher.Verify(request.Password, user.PasswordHash);

            if (user is null)
                passwordHasher.Hash(request.Password);

            if (user is null || !isPasswordValid)
            {
                if (user is not null)
                {
                    var settings = lockoutSettings.Value;
                    user.RegisterFailedLoginAttempt(settings.MaxFailedAttempts, TimeSpan.FromMinutes(settings.LockoutDurationMinutes));

                    userRepository.Update(user);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }

                throw new ValidationException("Credentials", "Invalid email or password");
            }

            user.EnsureActive();

            user.ResetFailedLoginAttempts();
            userRepository.Update(user);

            var accessToken = jwtService.GenerateAccessToken(user);
            var refreshToken = jwtService.GenerateRefreshToken();
            var refreshTokenHash = tokenHasher.Hash(refreshToken);

            var token = new RefreshTokenEntity(
                refreshTokenHash,
                user.Id,
                request.DeviceInfo);

            await refreshTokenRepository.AddAsync(token, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }
    }
}
