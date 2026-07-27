using blog.Domain.Common.Interfaces;
using blog.Domain.Common.Settings;
using blog.Domain.EmailVerifications.Entities;
using blog.Domain.EmailVerifications.Enums;
using blog.Domain.EmailVerifications.Extensions;
using blog.Domain.EmailVerifications.Repository;
using blog.Domain.Exceptions;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;
using Microsoft.Extensions.Options;

namespace blog.Application.Users.Commands.ResendChangeEmailCode
{
    public class ResendChangeEmailCodeCommandHandler(IUserRepository userRepository, IEmailVerificationRepository emailVerificationRepository, IEmailService emailService, IOptions<EmailVerificationSettings> emailVerificationSettings, IUnitOfWork unitOfWork) : IRequestHandler<ResendChangeEmailCodeCommand, ResendChangeEmailCodeResponse>
    {
        public async Task<ResendChangeEmailCodeResponse> Handle(ResendChangeEmailCodeCommand request, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByIdAsync(new UserId(request.UserId), cancellationToken);
            if (user is null)
                throw new NotFoundException("User", request.UserId);

            var verification = await emailVerificationRepository.GetActiveByUserIdAsync(user.Id, cancellationToken);
            if (verification is null || verification.Purpose is not (EmailVerificationPurpose.ChangeEmail or EmailVerificationPurpose.ConfirmNewEmail))
                throw new NotFoundException("EmailVerification", user.Id.Value);

            var purpose = verification.Purpose;
            var recipient = purpose == EmailVerificationPurpose.ChangeEmail ? user.Email : verification.TargetEmail!;

            if (!verification.IsValid())
            {
                verification.Revoke();
                emailVerificationRepository.Update(verification);

                var expiryMinutes = emailVerificationSettings.Value.GetExpiryMinutes(purpose);
                var codeHash = await emailService.SendVerificationCodeAsync(recipient, purpose, cancellationToken);

                var newVerification = new EmailVerification(user.Id, codeHash, purpose, expiryMinutes, verification.TargetEmail);
                await emailVerificationRepository.AddAsync(newVerification, cancellationToken);

                await unitOfWork.SaveChangesAsync(cancellationToken);

                return new ResendChangeEmailCodeResponse
                {
                    Success = true,
                    ExpiresAt = newVerification.ExpiresAt
                };
            }

            var maxAttempts = emailVerificationSettings.Value.GetMaxAttempts(purpose);
            if (verification.HasExceededAttempts(maxAttempts))
                throw new LockedException("EmailVerification", verification.ExpiresAt);

            return new ResendChangeEmailCodeResponse
            {
                Success = true,
                ExpiresAt = verification.ExpiresAt
            };
        }
    }
}