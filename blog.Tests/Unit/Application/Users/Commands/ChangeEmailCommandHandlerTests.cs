using blog.Application.Users.Commands.ChangeEmail;
using blog.Domain.Common.Interfaces;
using blog.Domain.Common.Settings;
using blog.Domain.EmailVerifications.Entities;
using blog.Domain.EmailVerifications.Enums;
using blog.Domain.EmailVerifications.Repository;
using blog.Domain.Exceptions;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace blog.Tests.Unit.Application.Users.Commands
{
    public class ChangeEmailCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IEmailVerificationRepository> _emailVerificationRepositoryMock = new();
        private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
        private readonly Mock<IEmailService> _emailServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly ChangeEmailCommandHandler _handler;

        private readonly EmailVerificationSettings _emailVerificationSettings = new()
        {
            RegistrationExpiryMinutes = 15,
            RegistrationMaxAttempts = 5,
            LoginVerificationExpiryMinutes = 10,
            LoginVerificationMaxAttempts = 5,
            ChangeEmailExpiryMinutes = 15,
            ChangeEmailMaxAttempts = 5,
            ResetPasswordExpiryMinutes = 15,
            ResetPasswordMaxAttempts = 5
        };

        public ChangeEmailCommandHandlerTests()
        {
            _handler = new ChangeEmailCommandHandler(
                _userRepositoryMock.Object,
                _emailVerificationRepositoryMock.Object,
                _passwordHasherMock.Object,
                _emailServiceMock.Object,
                Options.Create(_emailVerificationSettings),
                _unitOfWorkMock.Object);
        }

        private static User CreateConfirmedUser()
        {
            var user = new User("current@test.com", "Ali", "Rezaei", "hashed_password");
            user.ConfirmEmail();
            return user;
        }

        private static ChangeEmailCommand CreateCommand(Guid userId, string newEmail = "new@test.com") => new()
        {
            UserId = userId,
            NewEmail = newEmail,
            CurrentPassword = "CurrentPass123!"
        };

        // ── User not found ──────────────────────────────────────────────

        [Fact]
        public async Task Handle_UserNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var command = CreateCommand(Guid.NewGuid());

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(command.UserId), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        // ── Inactive / unconfirmed user ──────────────────────────────────

        [Fact]
        public async Task Handle_UserIsBanned_ThrowsInvalidStateException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            user.Ban();

            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_EmailNotConfirmed_ThrowsEmailNotConfirmedException()
        {
            // Arrange
            var user = new User("current@test.com", "Ali", "Rezaei", "hashed_password");
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<EmailNotConfirmedException>();
        }

        // ── Wrong current password ───────────────────────────────────────

        [Fact]
        public async Task Handle_InvalidCurrentPassword_ThrowsValidationException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.CurrentPassword, user.PasswordHash))
                .Returns(false);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task Handle_InvalidCurrentPassword_DoesNotSendVerificationCode()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.CurrentPassword, user.PasswordHash))
                .Returns(false);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<ValidationException>();

            // Assert
            _emailServiceMock.Verify(x => x.SendVerificationCodeAsync(
                It.IsAny<string>(), It.IsAny<EmailVerificationPurpose>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ── Same email as current ────────────────────────────────────────

        [Fact]
        public async Task Handle_NewEmailSameAsCurrent_ThrowsValidationException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value, newEmail: user.Email);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.CurrentPassword, user.PasswordHash))
                .Returns(true);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task Handle_NewEmailSameAsCurrentWithDifferentCasing_ThrowsValidationException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value, newEmail: user.Email.ToUpperInvariant());

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.CurrentPassword, user.PasswordHash))
                .Returns(true);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
        }

        // ── New email already taken ───────────────────────────────────────

        [Fact]
        public async Task Handle_NewEmailAlreadyTaken_ThrowsAlreadyExistsException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.CurrentPassword, user.PasswordHash))
                .Returns(true);

            _userRepositoryMock
                .Setup(x => x.ExistsByEmailAsync(command.NewEmail, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<AlreadyExistsException>();
        }

        // ── Already-active verification (both relevant purposes) ────────

        [Fact]
        public async Task Handle_ActiveChangeEmailVerificationExists_ThrowsAlreadyExistsException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.CurrentPassword, user.PasswordHash))
                .Returns(true);

            _userRepositoryMock
                .Setup(x => x.ExistsByEmailAsync(command.NewEmail, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var existingVerification = new EmailVerification(user.Id, "existing_hash", EmailVerificationPurpose.ChangeEmail, 15, "other@test.com");

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingVerification);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<AlreadyExistsException>();
        }

        [Fact]
        public async Task Handle_ActiveConfirmNewEmailVerificationExists_ThrowsAlreadyExistsException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.CurrentPassword, user.PasswordHash))
                .Returns(true);

            _userRepositoryMock
                .Setup(x => x.ExistsByEmailAsync(command.NewEmail, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var existingVerification = new EmailVerification(user.Id, "existing_hash", EmailVerificationPurpose.ConfirmNewEmail, 15, "other@test.com");

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingVerification);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<AlreadyExistsException>();
        }

        [Fact]
        public async Task Handle_ActiveVerificationWithUnrelatedPurposeExists_StillInitiatesChangeEmail()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.CurrentPassword, user.PasswordHash))
                .Returns(true);

            _userRepositoryMock
                .Setup(x => x.ExistsByEmailAsync(command.NewEmail, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var existingVerification = new EmailVerification(user.Id, "existing_hash", EmailVerificationPurpose.ResetPassword, 15);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingVerification);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.ChangeEmail, It.IsAny<CancellationToken>()))
                .ReturnsAsync("code_hash");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
        }

        // ── Successful initiate ──────────────────────────────────────────

        [Fact]
        public async Task Handle_ValidRequest_ReturnsSuccessResponseWithExpiryMinutes()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.CurrentPassword, user.PasswordHash))
                .Returns(true);

            _userRepositoryMock
                .Setup(x => x.ExistsByEmailAsync(command.NewEmail, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((EmailVerification?)null);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.ChangeEmail, It.IsAny<CancellationToken>()))
                .ReturnsAsync("code_hash");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.ExpiryMinutes.Should().Be(_emailVerificationSettings.ChangeEmailExpiryMinutes);
        }

        [Fact]
        public async Task Handle_ValidRequest_SendsVerificationCodeToCurrentEmail()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.CurrentPassword, user.PasswordHash))
                .Returns(true);

            _userRepositoryMock
                .Setup(x => x.ExistsByEmailAsync(command.NewEmail, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((EmailVerification?)null);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.ChangeEmail, It.IsAny<CancellationToken>()))
                .ReturnsAsync("code_hash");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _emailServiceMock.Verify(x => x.SendVerificationCodeAsync(
                user.Email, EmailVerificationPurpose.ChangeEmail, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidRequest_AddsEmailVerificationWithNewEmailAsTargetEmail()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.CurrentPassword, user.PasswordHash))
                .Returns(true);

            _userRepositoryMock
                .Setup(x => x.ExistsByEmailAsync(command.NewEmail, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((EmailVerification?)null);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.ChangeEmail, It.IsAny<CancellationToken>()))
                .ReturnsAsync("code_hash");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _emailVerificationRepositoryMock.Verify(x => x.AddAsync(It.Is<EmailVerification>(v =>
                v.UserId == user.Id &&
                v.Purpose == EmailVerificationPurpose.ChangeEmail &&
                v.CodeHash == "code_hash" &&
                v.TargetEmail == command.NewEmail.ToLowerInvariant()), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidRequest_SavesChanges()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.CurrentPassword, user.PasswordHash))
                .Returns(true);

            _userRepositoryMock
                .Setup(x => x.ExistsByEmailAsync(command.NewEmail, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((EmailVerification?)null);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.ChangeEmail, It.IsAny<CancellationToken>()))
                .ReturnsAsync("code_hash");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}