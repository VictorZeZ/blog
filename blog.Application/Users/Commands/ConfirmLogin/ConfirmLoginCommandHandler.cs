using blog.Domain.Common.Interfaces;
using blog.Domain.Common.Settings;
using blog.Domain.EmailVerifications.Enums;
using blog.Domain.EmailVerifications.Extensions;
using blog.Domain.EmailVerifications.Repository;
using blog.Domain.EmailVerifications.Types;
using blog.Domain.Exceptions;
using blog.Domain.Tokens.Repository;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using MediatR;
using Microsoft.Extensions.Options;
using RefreshTokenEntity = blog.Domain.Tokens.Entities.RefreshToken;

namespace blog.Application.Users.Commands.ConfirmLogin
{
    public class ConfirmLoginCommandHandler(IUserRepository userRepository, IEmailVerificationRepository emailVerificationRepository, IRefreshTokenRepository refreshTokenRepository, IHasher hasher, IJwtService jwtService, IOptions<EmailVerificationSettings> emailVerificationSettings, IUnitOfWork unitOfWork) : IRequestHandler<ConfirmLoginCommand, ConfirmLoginResponse>
    {
        public async Task<ConfirmLoginResponse> Handle(ConfirmLoginCommand request, CancellationToken cancellationToken)
        {
            var verification = await emailVerificationRepository.GetByIdAsync(new EmailVerificationId(request.ChallengeId), cancellationToken);
            if (verification is null || verification.Purpose != EmailVerificationPurpose.LoginVerification)
                throw new NotFoundException("EmailVerification", request.ChallengeId);

            if (!verification.IsValid())
                throw new ExpiredException("EmailVerification");

            var maxAttempts = emailVerificationSettings.Value.GetMaxAttempts(EmailVerificationPurpose.LoginVerification);
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

            var user = await userRepository.GetByIdAsync(verification.UserId, cancellationToken);
            if (user is null)
                throw new NotFoundException("User", verification.UserId.Value);

            user.EnsureActive();

            verification.MarkAsVerified();

            var accessToken = jwtService.GenerateAccessToken(user);
            var refreshToken = jwtService.GenerateRefreshToken();
            var refreshTokenHash = hasher.Hash(refreshToken);

            var token = new RefreshTokenEntity(refreshTokenHash, user.Id, request.DeviceInfo);

            emailVerificationRepository.Update(verification);
            await refreshTokenRepository.AddAsync(token, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new ConfirmLoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }
    }
}
