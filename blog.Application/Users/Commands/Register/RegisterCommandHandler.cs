using blog.Domain.Common.Interfaces;
using blog.Domain.Common.Settings;
using blog.Domain.EmailVerifications.Entities;
using blog.Domain.EmailVerifications.Enums;
using blog.Domain.EmailVerifications.Extensions;
using blog.Domain.EmailVerifications.Repository;
using blog.Domain.Exceptions;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Repository;
using MediatR;
using Microsoft.Extensions.Options;

namespace blog.Application.Users.Commands.Register
{
    public class RegisterCommandHandler(IUserRepository userRepository, IEmailVerificationRepository emailVerificationRepository, IPasswordHasher passwordHasher, IEmailService emailService, IOptions<EmailVerificationSettings> emailVerificationSettings, IUnitOfWork unitOfWork) : IRequestHandler<RegisterCommand, RegisterResponse>
    {
        public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var exists = await userRepository.ExistsByEmailAsync(request.Email, cancellationToken);
            if (exists)
                throw new AlreadyExistsException("User", request.Email);

            var passwordHash = passwordHasher.Hash(request.Password);

            var user = new User(
                request.Email,
                request.FirstName,
                request.LastName,
                passwordHash);

            await userRepository.AddAsync(user, cancellationToken);

            var expiryMinutes = emailVerificationSettings.Value.GetExpiryMinutes(EmailVerificationPurpose.Registration);
            var codeHash = await emailService.SendVerificationCodeAsync(user.Email, expiryMinutes, cancellationToken);

            var verification = new EmailVerification(user.Id, codeHash, EmailVerificationPurpose.Registration, expiryMinutes);
            await emailVerificationRepository.AddAsync(verification, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new RegisterResponse
            {
                Id = user.Id.Value,
                Email = user.Email,
                ExpiryMinutes = expiryMinutes
            };
        }
    }
}
