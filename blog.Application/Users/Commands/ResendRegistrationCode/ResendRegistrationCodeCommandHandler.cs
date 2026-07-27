using blog.Domain.Common.Interfaces;
using blog.Domain.Common.Settings;
using blog.Domain.EmailVerifications.Entities;
using blog.Domain.EmailVerifications.Enums;
using blog.Domain.EmailVerifications.Extensions;
using blog.Domain.EmailVerifications.Repository;
using blog.Domain.Exceptions;
using blog.Domain.Users.Repository;
using MediatR;
using Microsoft.Extensions.Options;

namespace blog.Application.Users.Commands.ResendRegistrationCode
{
    public class ResendRegistrationCodeCommandHandler(IUserRepository userRepository, IEmailVerificationRepository emailVerificationRepository, IEmailService emailService, IOptions<EmailVerificationSettings> emailVerificationSettings, IUnitOfWork unitOfWork) : IRequestHandler<ResendRegistrationCodeCommand, ResendRegistrationCodeResponse>
    {
        public async Task<ResendRegistrationCodeResponse> Handle(ResendRegistrationCodeCommand request, CancellationToken cancellationToken)
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
            {
                verification.Revoke();
                emailVerificationRepository.Update(verification);

                var expiryMinutes = emailVerificationSettings.Value.GetExpiryMinutes(EmailVerificationPurpose.Registration);
                var codeHash = await emailService.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.Registration, cancellationToken);

                var newVerification = new EmailVerification(user.Id, codeHash, EmailVerificationPurpose.Registration, expiryMinutes);
                await emailVerificationRepository.AddAsync(newVerification, cancellationToken);

                await unitOfWork.SaveChangesAsync(cancellationToken);

                return new ResendRegistrationCodeResponse
                {
                    Success = true,
                    ExpiresAt = newVerification.ExpiresAt
                };
            }

            var maxAttempts = emailVerificationSettings.Value.GetMaxAttempts(EmailVerificationPurpose.Registration);
            if (verification.HasExceededAttempts(maxAttempts))
                throw new LockedException("EmailVerification", verification.ExpiresAt);

            return new ResendRegistrationCodeResponse
            {
                Success = true,
                ExpiresAt = verification.ExpiresAt
            };
        }
    }
}
