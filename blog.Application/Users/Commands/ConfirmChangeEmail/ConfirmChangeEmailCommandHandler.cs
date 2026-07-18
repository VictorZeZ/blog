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

namespace blog.Application.Users.Commands.ConfirmChangeEmail
{
    public class ConfirmChangeEmailCommandHandler(IUserRepository userRepository, IEmailVerificationRepository emailVerificationRepository, IHasher hasher, IEmailService emailService, IOptions<EmailVerificationSettings> emailVerificationSettings, IUnitOfWork unitOfWork) : IRequestHandler<ConfirmChangeEmailCommand, ConfirmChangeEmailResponse>
    {
        public async Task<ConfirmChangeEmailResponse> Handle(ConfirmChangeEmailCommand request, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByIdAsync(new UserId(request.UserId), cancellationToken);
            if (user is null)
                throw new NotFoundException("User", request.UserId);

            var verification = await emailVerificationRepository.GetActiveByUserIdAsync(user.Id, cancellationToken);
            if (verification is null || verification.Purpose != EmailVerificationPurpose.ChangeEmail)
                throw new NotFoundException("EmailVerification", user.Id.Value);

            if (!verification.IsValid())
                throw new ExpiredException("EmailVerification");

            var maxAttempts = emailVerificationSettings.Value.GetMaxAttempts(EmailVerificationPurpose.ChangeEmail);
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

            var targetEmail = verification.TargetEmail!;

            var emailTaken = await userRepository.ExistsByEmailAsync(targetEmail, cancellationToken);
            if (emailTaken)
                throw new AlreadyExistsException("User", targetEmail);

            verification.MarkAsVerified();

            var expiryMinutes = emailVerificationSettings.Value.GetExpiryMinutes(EmailVerificationPurpose.ConfirmNewEmail);
            var newCodeHash = await emailService.SendVerificationCodeAsync(targetEmail, EmailVerificationPurpose.ConfirmNewEmail, cancellationToken);

            var nextVerification = new EmailVerification(user.Id, newCodeHash, EmailVerificationPurpose.ConfirmNewEmail, expiryMinutes, targetEmail);
            await emailVerificationRepository.AddAsync(nextVerification, cancellationToken);

            emailVerificationRepository.Update(verification);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new ConfirmChangeEmailResponse
            {
                Success = true,
                ExpiryMinutes = expiryMinutes
            };
        }
    }
}
