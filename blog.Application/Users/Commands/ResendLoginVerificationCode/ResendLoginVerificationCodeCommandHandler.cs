using blog.Domain.Common.Interfaces;
using blog.Domain.Common.Settings;
using blog.Domain.EmailVerifications.Entities;
using blog.Domain.EmailVerifications.Enums;
using blog.Domain.EmailVerifications.Extensions;
using blog.Domain.EmailVerifications.Repository;
using blog.Domain.EmailVerifications.Types;
using blog.Domain.Exceptions;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using MediatR;
using Microsoft.Extensions.Options;

namespace blog.Application.Users.Commands.ResendLoginVerificationCode
{
    public class ResendLoginVerificationCodeCommandHandler(IUserRepository userRepository, IEmailVerificationRepository emailVerificationRepository, IEmailService emailService, IOptions<EmailVerificationSettings> emailVerificationSettings, IUnitOfWork unitOfWork) : IRequestHandler<ResendLoginVerificationCodeCommand, ResendLoginVerificationCodeResponse>
    {
        public async Task<ResendLoginVerificationCodeResponse> Handle(ResendLoginVerificationCodeCommand request, CancellationToken cancellationToken)
        {
            var verification = await emailVerificationRepository.GetByIdAsync(new EmailVerificationId(request.ChallengeId), cancellationToken);
            if (verification is null || verification.Purpose != EmailVerificationPurpose.LoginVerification)
                throw new NotFoundException("EmailVerification", request.ChallengeId);

            var user = await userRepository.GetByIdAsync(verification.UserId, cancellationToken);
            if (user is null)
                throw new NotFoundException("User", verification.UserId.Value);

            user.EnsureActive();

            if (!verification.IsValid())
            {
                verification.Revoke();
                emailVerificationRepository.Update(verification);

                var expiryMinutes = emailVerificationSettings.Value.GetExpiryMinutes(EmailVerificationPurpose.LoginVerification);
                var codeHash = await emailService.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.LoginVerification, cancellationToken);

                var newVerification = new EmailVerification(user.Id, codeHash, EmailVerificationPurpose.LoginVerification, expiryMinutes);
                await emailVerificationRepository.AddAsync(newVerification, cancellationToken);

                await unitOfWork.SaveChangesAsync(cancellationToken);

                return new ResendLoginVerificationCodeResponse
                {
                    Success = true,
                    ChallengeId = newVerification.Id.Value,
                    ExpiresAt = newVerification.ExpiresAt
                };
            }

            var maxAttempts = emailVerificationSettings.Value.GetMaxAttempts(EmailVerificationPurpose.LoginVerification);
            if (verification.HasExceededAttempts(maxAttempts))
                throw new LockedException("EmailVerification", verification.ExpiresAt);

            return new ResendLoginVerificationCodeResponse
            {
                Success = true,
                ChallengeId = verification.Id.Value,
                ExpiresAt = verification.ExpiresAt
            };
        }
    }
}