using blog.Domain.Common.Interfaces;
using blog.Domain.Common.Settings;
using blog.Domain.EmailVerifications.Entities;
using blog.Domain.EmailVerifications.Enums;
using blog.Domain.EmailVerifications.Extensions;
using blog.Domain.EmailVerifications.Repository;
using blog.Domain.Users.Repository;
using MediatR;
using Microsoft.Extensions.Options;

namespace blog.Application.Users.Commands.ForgotPassword
{
    public class ForgotPasswordCommandHandler(IUserRepository userRepository, IEmailVerificationRepository emailVerificationRepository, IEmailService emailService, IOptions<EmailVerificationSettings> emailVerificationSettings, IUnitOfWork unitOfWork) : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponse>
    {
        public async Task<ForgotPasswordResponse> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

            if (IsEligibleForReset(user))
            {
                var existingVerification = await emailVerificationRepository.GetActiveByUserIdAsync(user!.Id, cancellationToken);

                if (existingVerification is null || existingVerification.Purpose != EmailVerificationPurpose.ResetPassword)
                {
                    var expiryMinutes = emailVerificationSettings.Value.GetExpiryMinutes(EmailVerificationPurpose.ResetPassword);
                    var codeHash = await emailService.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.ResetPassword, cancellationToken);

                    var verification = new EmailVerification(user.Id, codeHash, EmailVerificationPurpose.ResetPassword, expiryMinutes);
                    await emailVerificationRepository.AddAsync(verification, cancellationToken);

                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            // Always return the same generic response regardless of whether the email exists,
            // is confirmed, is active, or already has a pending reset code — prevents user enumeration.
            return new ForgotPasswordResponse { Success = true };
        }

        private static bool IsEligibleForReset(Domain.Users.Entities.User? user)
            => user is not null && !user.IsDeleted && !user.IsBanned && user.IsEmailConfirmed;
    }
}
