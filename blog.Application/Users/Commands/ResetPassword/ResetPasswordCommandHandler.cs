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

namespace blog.Application.Users.Commands.ResetPassword
{
    public class ResetPasswordCommandHandler(IUserRepository userRepository, IEmailVerificationRepository emailVerificationRepository, IRefreshTokenRepository refreshTokenRepository, IPasswordHasher passwordHasher, IHasher hasher, IOptions<EmailVerificationSettings> emailVerificationSettings, IUnitOfWork unitOfWork) : IRequestHandler<ResetPasswordCommand, ResetPasswordResponse>
    {
        public async Task<ResetPasswordResponse> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (user is null)
                throw new NotFoundException("User", request.Email);

            var verification = await emailVerificationRepository.GetActiveByUserIdAsync(user.Id, cancellationToken);
            if (verification is null || verification.Purpose != EmailVerificationPurpose.ResetPassword)
                throw new NotFoundException("EmailVerification", user.Id.Value);

            if (!verification.IsValid())
                throw new ExpiredException("EmailVerification");

            var maxAttempts = emailVerificationSettings.Value.GetMaxAttempts(EmailVerificationPurpose.ResetPassword);
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

            var newPasswordHash = passwordHasher.Hash(request.NewPassword);
            user.ChangePassword(newPasswordHash);
            userRepository.Update(user);

            var activeTokens = await refreshTokenRepository.GetActiveByUserIdAsync(user.Id, cancellationToken);
            foreach (var token in activeTokens)
            {
                token.Revoke();
                refreshTokenRepository.Update(token);
            }

            emailVerificationRepository.Update(verification);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new ResetPasswordResponse { Success = true };
        }
    }
}
