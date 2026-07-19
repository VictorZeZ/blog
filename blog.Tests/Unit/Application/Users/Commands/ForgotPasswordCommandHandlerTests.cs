using blog.Application.Users.Commands.ForgotPassword;
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
    public class ForgotPasswordCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IEmailVerificationRepository> _emailVerificationRepositoryMock = new();
        private readonly Mock<IEmailService> _emailServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly ForgotPasswordCommandHandler _handler;

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

        public ForgotPasswordCommandHandlerTests()
        {
            _handler = new ForgotPasswordCommandHandler(
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

        private static ForgotPasswordCommand CreateCommand(string email = "test@test.com") => new()
        {
            Email = email
        };

        // ── User does not exist ──────────────────────────────────────────

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
        public async Task Handle_UserNotFound_DoesNotSendVerificationCode()
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

        // ── Email not confirmed ──────────────────────────────────────────

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

        // ── Banned user ───────────────────────────────────────────────────

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

        // ── Deleted user ──────────────────────────────────────────────────

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

        // ── Already has an active ResetPassword verification ────────────

        [Fact]
        public async Task Handle_ActiveResetPasswordVerificationExists_ReturnsGenericSuccessResponseWithoutSendingNewCode()
        {
            // Arrange
            var user = CreateEligibleUser();
            var command = CreateCommand(user.Email);

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var existingVerification = new EmailVerification(user.Id, "existing_hash", EmailVerificationPurpose.ResetPassword, 15);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingVerification);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            _emailServiceMock.Verify(x => x.SendVerificationCodeAsync(
                It.IsAny<string>(), It.IsAny<EmailVerificationPurpose>(), It.IsAny<CancellationToken>()), Times.Never);
            _emailVerificationRepositoryMock.Verify(x => x.AddAsync(
                It.IsAny<EmailVerification>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ── Active verification exists but for a different purpose ─────

        [Fact]
        public async Task Handle_ActiveVerificationWithDifferentPurposeExists_StillSendsNewResetPasswordCode()
        {
            // Arrange
            var user = CreateEligibleUser();
            var command = CreateCommand(user.Email);

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var existingVerification = new EmailVerification(user.Id, "existing_hash", EmailVerificationPurpose.ChangeEmail, 15, "new@test.com");

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingVerification);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.ResetPassword, It.IsAny<CancellationToken>()))
                .ReturnsAsync("new_hash");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            _emailVerificationRepositoryMock.Verify(x => x.AddAsync(
                It.Is<EmailVerification>(v => v.Purpose == EmailVerificationPurpose.ResetPassword),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        // ── Eligible user, no active verification ───────────────────────

        [Fact]
        public async Task Handle_EligibleUserWithNoActiveVerification_ReturnsGenericSuccessResponse()
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

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.ResetPassword, It.IsAny<CancellationToken>()))
                .ReturnsAsync("code_hash");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_EligibleUserWithNoActiveVerification_SendsVerificationCodeWithResetPasswordPurpose()
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

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.ResetPassword, It.IsAny<CancellationToken>()))
                .ReturnsAsync("code_hash");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _emailServiceMock.Verify(x => x.SendVerificationCodeAsync(
                user.Email, EmailVerificationPurpose.ResetPassword, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_EligibleUserWithNoActiveVerification_AddsEmailVerificationWithCorrectData()
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

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.ResetPassword, It.IsAny<CancellationToken>()))
                .ReturnsAsync("code_hash");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _emailVerificationRepositoryMock.Verify(x => x.AddAsync(It.Is<EmailVerification>(v =>
                v.UserId == user.Id &&
                v.Purpose == EmailVerificationPurpose.ResetPassword &&
                v.CodeHash == "code_hash" &&
                v.TargetEmail == null), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_EligibleUserWithNoActiveVerification_SavesChanges()
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

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.ResetPassword, It.IsAny<CancellationToken>()))
                .ReturnsAsync("code_hash");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}