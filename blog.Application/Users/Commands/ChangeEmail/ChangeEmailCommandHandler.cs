using blog.Domain.Common.Helpers;
using blog.Domain.Common.Interfaces;
using blog.Domain.Common.Settings;
using blog.Domain.EmailVerifications.Entities;
using blog.Domain.EmailVerifications.Enums;
using blog.Domain.EmailVerifications.Extensions;
using blog.Domain.EmailVerifications.Repository;
using blog.Domain.Exceptions;
using blog.Domain.Users.Extensions;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using MediatR;
using Microsoft.Extensions.Options;

namespace blog.Application.Users.Commands.ChangeEmail
{
    public class ChangeEmailCommandHandler(IUserRepository userRepository, IEmailVerificationRepository emailVerificationRepository, IPasswordHasher passwordHasher, IEmailService emailService, IOptions<EmailVerificationSettings> emailVerificationSettings, IUnitOfWork unitOfWork) : IRequestHandler<ChangeEmailCommand, ChangeEmailResponse>
    {
        public async Task<ChangeEmailResponse> Handle(ChangeEmailCommand request, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByIdAsync(new UserId(request.UserId), cancellationToken);
            if (user is null)
                throw new NotFoundException("User", request.UserId);

            user.EnsureActive();
            user.EnsureEmailConfirmed();

            var isPasswordValid = passwordHasher.Verify(request.CurrentPassword, user.PasswordHash);
            if (!isPasswordValid)
                throw new ValidationException("CurrentPassword", "Current password is incorrect");

            if (EmailNormalizer.Normalize(request.NewEmail) == user.Email)
                throw new ValidationException("NewEmail", "New email must be different from the current email");

            var emailTaken = await userRepository.ExistsByEmailAsync(request.NewEmail, cancellationToken);
            if (emailTaken)
                throw new AlreadyExistsException("User", request.NewEmail);

            var existingVerification = await emailVerificationRepository.GetActiveByUserIdAsync(user.Id, cancellationToken);
            if (existingVerification is not null &&
                existingVerification.Purpose is EmailVerificationPurpose.ChangeEmail or EmailVerificationPurpose.ConfirmNewEmail)
                throw new AlreadyExistsException("EmailVerification", existingVerification.Id.Value);

            var expiryMinutes = emailVerificationSettings.Value.GetExpiryMinutes(EmailVerificationPurpose.ChangeEmail);
            var codeHash = await emailService.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.ChangeEmail, cancellationToken);

            var verification = new EmailVerification(user.Id, codeHash, EmailVerificationPurpose.ChangeEmail, expiryMinutes, request.NewEmail);
            await emailVerificationRepository.AddAsync(verification, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new ChangeEmailResponse
            {
                Success = true,
                ExpiryMinutes = expiryMinutes
            };
        }
    }
}
