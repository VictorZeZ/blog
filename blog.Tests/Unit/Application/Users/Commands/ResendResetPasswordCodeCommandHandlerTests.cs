using blog.Application.Users.Commands.ResendResetPasswordCode;
using blog.Domain.Common.Interfaces;
using blog.Domain.Common.Settings;
using blog.Domain.EmailVerifications.Entities;
using blog.Domain.EmailVerifications.Enums;
using blog.Domain.EmailVerifications.Repository;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Repository;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace blog.Tests.Unit.Application.Users.Commands
{
    public class ResendResetPasswordCodeCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IEmailVerificationRepository> _emailVerificationRepositoryMock = new();
        private readonly Mock<IEmailService> _emailServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly ResendResetPasswordCodeCommandHandler _handler;

        private readonly EmailVerificationSettings _emailVerificationSettings = new()
        {
            RegistrationExpiryMinutes = 15,
            RegistrationMaxAttempts = 5,
            LoginVerificationExpiryMinutes = 10,
            LoginVerificationMaxAttempts = 5,
            ChangeEmailExpiryMinutes = 15,
            ChangeEmailMaxAttempts = 5,
            ResetPasswordExpiryMinutes = 15,
            ResetPasswordMaxAttempts = 5,
            ConfirmNewEmailExpiryMinutes = 15,
            ConfirmNewEmailMaxAttempts = 5
        };

        public ResendResetPasswordCodeCommandHandlerTests()
        {
            _handler = new ResendResetPasswordCodeCommandHandler(
                _userRepositoryMock.Object,
                _emailVerificationRepositoryMock.Object,
                _emailServiceMock.Object,
                Options.Create(_emailVerificationSettings),
                _unitOfWorkMock.Object);
        }

        private static User CreateEligibleUser()
        {
            var user = new User("test@test.com", "Ali", "Rezaei", "hashed_password");
            user.ConfirmEmail();
            return user;
        }

        private static ResendResetPasswordCodeCommand CreateCommand(string email = "test@test.com") => new()
        {
            Email = email
        };

        // ── User not found ──────────────────────────────────────────────

        [Fact]
        public async Task Handle_UserNotFound_ReturnsGenericSuccessResponse()
        {
            // Arrange
            var command = CreateCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_UserNotFound_DoesNotSendCode()
        {
            // Arrange
            var command = CreateCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _emailServiceMock.Verify(x => x.SendVerificationCodeAsync(
                It.IsAny<string>(), It.IsAny<EmailVerificationPurpose>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ── Ineligible user states ───────────────────────────────────────

        [Fact]
        public async Task Handle_EmailNotConfirmed_ReturnsGenericSuccessResponseWithoutSendingCode()
        {
            // Arrange
            var user = new User("test@test.com", "Ali", "Rezaei", "hashed_password");
            var command = CreateCommand(user.Email);

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            _emailServiceMock.Verify(x => x.SendVerificationCodeAsync(
                It.IsAny<string>(), It.IsAny<EmailVerificationPurpose>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_UserIsBanned_ReturnsGenericSuccessResponseWithoutSendingCode()
        {
            // Arrange
            var user = CreateEligibleUser();
            user.Ban();
            var command = CreateCommand(user.Email);

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            _emailServiceMock.Verify(x => x.SendVerificationCodeAsync(
                It.IsAny<string>(), It.IsAny<EmailVerificationPurpose>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_UserIsDeleted_ReturnsGenericSuccessResponseWithoutSendingCode()
        {
            // Arrange
            var user = CreateEligibleUser();
            user.SoftDelete();
            var command = CreateCommand(user.Email);

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            _emailServiceMock.Verify(x => x.SendVerificationCodeAsync(
                It.IsAny<string>(), It.IsAny<EmailVerificationPurpose>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ── No active verification / wrong purpose ──────────────────────

        [Fact]
        public async Task Handle_NoActiveVerification_ReturnsGenericSuccessResponseWithoutSendingCode()
        {
            // Arrange
            var user = CreateEligibleUser();
            var command = CreateCommand(user.Email);

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((EmailVerification?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            _emailServiceMock.Verify(x => x.SendVerificationCodeAsync(
                It.IsAny<string>(), It.IsAny<EmailVerificationPurpose>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ActiveVerificationHasDifferentPurpose_ReturnsGenericSuccessResponseWithoutSendingCode()
        {
            // Arrange
            var user = CreateEligibleUser();
            var command = CreateCommand(user.Email);

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "code_hash", EmailVerificationPurpose.ChangeEmail, 15, "new@test.com");

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            _emailServiceMock.Verify(x => x.SendVerificationCodeAsync(
                It.IsAny<string>(), It.IsAny<EmailVerificationPurpose>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ── Still valid (whether or not attempts exhausted) → silent no-op ──

        [Fact]
        public async Task Handle_StillValidVerification_ReturnsGenericSuccessResponseWithoutSendingNewCode()
        {
            // Arrange
            var user = CreateEligibleUser();
            var command = CreateCommand(user.Email);

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "code_hash", EmailVerificationPurpose.ResetPassword, 15);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            _emailServiceMock.Verify(x => x.SendVerificationCodeAsync(
                It.IsAny<string>(), It.IsAny<EmailVerificationPurpose>(), It.IsAny<CancellationToken>()), Times.Never);
            _emailVerificationRepositoryMock.Verify(x => x.Update(It.IsAny<EmailVerification>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ExceededAttemptsButStillValid_ReturnsGenericSuccessResponseWithoutThrowingOrSendingCode()
        {
            // Arrange
            var user = CreateEligibleUser();
            var command = CreateCommand(user.Email);

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "code_hash", EmailVerificationPurpose.ResetPassword, 15);
            for (var i = 0; i < _emailVerificationSettings.ResetPasswordMaxAttempts; i++)
                verification.RegisterFailedAttempt();

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert — must never throw LockedException here, that would leak state
            result.Success.Should().BeTrue();
            _emailServiceMock.Verify(x => x.SendVerificationCodeAsync(
                It.IsAny<string>(), It.IsAny<EmailVerificationPurpose>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ── Expired → reissue silently ────────────────────────────────────

        [Fact]
        public async Task Handle_ExpiredVerification_ReturnsGenericSuccessResponse()
        {
            // Arrange
            var user = CreateEligibleUser();
            var command = CreateCommand(user.Email);

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "old_hash", EmailVerificationPurpose.ResetPassword, -1);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.ResetPassword, It.IsAny<CancellationToken>()))
                .ReturnsAsync("new_hash");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ExpiredVerification_RevokesOldAndSendsNewCode()
        {
            // Arrange
            var user = CreateEligibleUser();
            var command = CreateCommand(user.Email);

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "old_hash", EmailVerificationPurpose.ResetPassword, -1);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.ResetPassword, It.IsAny<CancellationToken>()))
                .ReturnsAsync("new_hash");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _emailVerificationRepositoryMock.Verify(x => x.Update(verification), Times.Once);
            _emailServiceMock.Verify(x => x.SendVerificationCodeAsync(
                user.Email, EmailVerificationPurpose.ResetPassword, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ExpiredVerification_AddsNewVerificationWithFreshExpiry()
        {
            // Arrange
            var user = CreateEligibleUser();
            var command = CreateCommand(user.Email);

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "old_hash", EmailVerificationPurpose.ResetPassword, -1);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.ResetPassword, It.IsAny<CancellationToken>()))
                .ReturnsAsync("new_hash");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _emailVerificationRepositoryMock.Verify(x => x.AddAsync(It.Is<EmailVerification>(v =>
                v.UserId == user.Id &&
                v.Purpose == EmailVerificationPurpose.ResetPassword &&
                v.CodeHash == "new_hash" &&
                v.IsValid()), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ExpiredVerification_SavesChanges()
        {
            // Arrange
            var user = CreateEligibleUser();
            var command = CreateCommand(user.Email);

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "old_hash", EmailVerificationPurpose.ResetPassword, -1);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.ResetPassword, It.IsAny<CancellationToken>()))
                .ReturnsAsync("new_hash");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}