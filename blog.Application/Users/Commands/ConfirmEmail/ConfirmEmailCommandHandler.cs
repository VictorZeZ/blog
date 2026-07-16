using blog.Domain.Common.Interfaces;
using blog.Domain.Common.Settings;
using blog.Domain.EmailVerifications.Enums;
using blog.Domain.EmailVerifications.Extensions;
using blog.Domain.EmailVerifications.Repository;
using blog.Domain.Exceptions;
using blog.Domain.Tokens.Repository;
using blog.Domain.Users.Repository;
using MediatR;
using Microsoft.Extensions.Options;
using RefreshTokenEntity = blog.Domain.Tokens.Entities.RefreshToken;

namespace blog.Application.Users.Commands.ConfirmEmail
{
    public class ConfirmEmailCommandHandler(IUserRepository userRepository, IEmailVerificationRepository emailVerificationRepository, IRefreshTokenRepository refreshTokenRepository, IHasher hasher, IJwtService jwtService, IOptions<EmailVerificationSettings> emailVerificationSettings, IUnitOfWork unitOfWork) : IRequestHandler<ConfirmEmailCommand, ConfirmEmailResponse>
    {
        public async Task<ConfirmEmailResponse> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (user is null)
                throw new NotFoundException("User", request.Email);

            if (user.IsEmailConfirmed)
                throw new InvalidStateException("User", "Confirmed", "Unconfirmed");

            var verification = await emailVerificationRepository.GetActiveByUserIdAsync(user.Id, cancellationToken);
            if (verification is null || verification.Purpose != EmailVerificationPurpose.Registration)
                throw new NotFoundException("EmailVerification", user.Id.Value);

            if (!verification.IsValid())
                throw new ExpiredException("EmailVerification");

            var maxAttempts = emailVerificationSettings.Value.GetMaxAttempts(EmailVerificationPurpose.Registration);
            if (verification.HasExceededAttempts(maxAttempts))
                throw new LockedException("EmailVerification", verification.ExpiresAt);

            var codeHash = hasher.Hash(request.Code.ToUpper());
            if (codeHash != verification.CodeHash)
            {
                verification.RegisterFailedAttempt();
                emailVerificationRepository.Update(verification);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                throw new ValidationException("Code", "Invalid verification code");
            }

            verification.MarkAsVerified();
            user.ConfirmEmail();

            var accessToken = jwtService.GenerateAccessToken(user);
            var refreshToken = jwtService.GenerateRefreshToken();
            var refreshTokenHash = hasher.Hash(refreshToken);

            var token = new RefreshTokenEntity(refreshTokenHash, user.Id, request.DeviceInfo);

            emailVerificationRepository.Update(verification);
            userRepository.Update(user);
            await refreshTokenRepository.AddAsync(token, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new ConfirmEmailResponse
            {
                Id = user.Id.Value,
                Email = user.Email,
                FullName = user.FullName,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }
    }
}
