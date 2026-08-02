using blog.Domain.Common.Interfaces;
using blog.Domain.Common.Settings;
using blog.Domain.EmailVerifications.Entities;
using blog.Domain.EmailVerifications.Enums;
using blog.Domain.EmailVerifications.Extensions;
using blog.Domain.EmailVerifications.Repository;
using blog.Domain.Users.Repository;
using MediatR;
using Microsoft.Extensions.Options;

namespace blog.Application.Users.Commands.ResendResetPasswordCode
{
    public class ResendResetPasswordCodeCommandHandler(IUserRepository userRepository, IEmailVerificationRepository emailVerificationRepository, IEmailService emailService, IOptions<EmailVerificationSettings> emailVerificationSettings, IUnitOfWork unitOfWork) : IRequestHandler<ResendResetPasswordCodeCommand, ResendResetPasswordCodeResponse>
    {
        public async Task<ResendResetPasswordCodeResponse> Handle(ResendResetPasswordCodeCommand request, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

            var expiryMinutes = emailVerificationSettings.Value.GetExpiryMinutes(EmailVerificationPurpose.ResetPassword);
            if (IsEligibleForReset(user))
            {
                var verification = await emailVerificationRepository.GetActiveByUserIdAsync(user!.Id, cancellationToken);

                if (verification is not null && verification.Purpose == EmailVerificationPurpose.ResetPassword && !verification.IsValid())
                {
                    verification.Revoke();
                    emailVerificationRepository.Update(verification);

                    var codeHash = await emailService.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.ResetPassword, cancellationToken);

                    var newVerification = new EmailVerification(user.Id, codeHash, EmailVerificationPurpose.ResetPassword, expiryMinutes);
                    await emailVerificationRepository.AddAsync(newVerification, cancellationToken);

                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }

                // If a valid (non-expired) code already exists, or no ResetPassword flow is active at all,
                // do nothing — never differentiate the response, to avoid leaking account/state information.
            }

            return new ResendResetPasswordCodeResponse
            {
                Success = true,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes)
            };
        }

        private static bool IsEligibleForReset(Domain.Users.Entities.User? user)
            => user is not null && !user.IsDeleted && !user.IsBanned && user.IsEmailConfirmed;
    }
}
