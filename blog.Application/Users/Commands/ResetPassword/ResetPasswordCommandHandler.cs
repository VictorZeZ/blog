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
        private const string InvalidCodeReason = "Invalid or expired verification code";

        public async Task<ResetPasswordResponse> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var maxAttempts = emailVerificationSettings.Value.GetMaxAttempts(EmailVerificationPurpose.ResetPassword);

            var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
            var verification = user is null
                ? null
                : await emailVerificationRepository.GetActiveByUserIdAsync(user.Id, [EmailVerificationPurpose.ResetPassword], cancellationToken);

            if (user is null || verification is null || !verification.IsValid() || verification.HasExceededAttempts(maxAttempts))
                throw new ValidationException("Code", InvalidCodeReason);

            var codeHash = hasher.Hash(request.Code.ToUpper());
            if (codeHash != verification.CodeHash)
            {
                verification.RegisterFailedAttempt();

                if (verification.HasExceededAttempts(maxAttempts))
                    verification.Revoke();

                emailVerificationRepository.Update(verification);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                throw new ValidationException("Code", InvalidCodeReason);
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