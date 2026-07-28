using blog.Application.Users.Commands.ResendRegistrationCode;
using blog.Domain.Common.Interfaces;
using blog.Domain.Common.Settings;
using blog.Domain.EmailVerifications.Entities;
using blog.Domain.EmailVerifications.Enums;
using blog.Domain.EmailVerifications.Repository;
using blog.Domain.Exceptions;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Repository;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace blog.Tests.Unit.Application.Users.Commands
{
    public class ResendRegistrationCodeCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IEmailVerificationRepository> _emailVerificationRepositoryMock = new();
        private readonly Mock<IEmailService> _emailServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly ResendRegistrationCodeCommandHandler _handler;

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

        public ResendRegistrationCodeCommandHandlerTests()
        {
            _handler = new ResendRegistrationCodeCommandHandler(
                _userRepositoryMock.Object,
                _emailVerificationRepositoryMock.Object,
                _emailServiceMock.Object,
                Options.Create(_emailVerificationSettings),
                _unitOfWorkMock.Object);
        }

        private static User CreateUnconfirmedUser()
            => new("test@test.com", "Ali", "Rezaei", "hashed_password");

        private static ResendRegistrationCodeCommand CreateCommand(string email = "test@test.com") => new()
        {
            Email = email
        };

        // ── User not found ──────────────────────────────────────────────

        [Fact]
        public async Task Handle_UserNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var command = CreateCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        // ── Already confirmed ────────────────────────────────────────────

        [Fact]
        public async Task Handle_EmailAlreadyConfirmed_ThrowsInvalidStateException()
        {
            // Arrange
            var user = CreateUnconfirmedUser();
            user.ConfirmEmail();
            var command = CreateCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        // ── Missing / wrong-purpose verification ────────────────────────

        [Fact]
        public async Task Handle_NoActiveVerification_ThrowsNotFoundException()
        {
            // Arrange
            var user = CreateUnconfirmedUser();
            var command = CreateCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((EmailVerification?)null);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_ActiveVerificationHasDifferentPurpose_ThrowsNotFoundException()
        {
            // Arrange
            var user = CreateUnconfirmedUser();
            var command = CreateCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "code_hash", EmailVerificationPurpose.ResetPassword, 15);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        // ── Still valid, not exceeded → no reissue ──────────────────────

        [Fact]
        public async Task Handle_StillValidAndNotExceeded_ReturnsExistingExpiresAtWithoutSendingNewCode()
        {
            // Arrange
            var user = CreateUnconfirmedUser();
            var command = CreateCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "code_hash", EmailVerificationPurpose.Registration, 15);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.ExpiresAt.Should().Be(verification.ExpiresAt);
            _emailServiceMock.Verify(x => x.SendVerificationCodeAsync(
                It.IsAny<string>(), It.IsAny<EmailVerificationPurpose>(), It.IsAny<CancellationToken>()), Times.Never);
            _emailVerificationRepositoryMock.Verify(x => x.AddAsync(
                It.IsAny<EmailVerification>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ── Exceeded attempts, still valid → locked ─────────────────────

        [Fact]
        public async Task Handle_ExceededMaxAttemptsButStillValid_ThrowsLockedException()
        {
            // Arrange
            var user = CreateUnconfirmedUser();
            var command = CreateCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "code_hash", EmailVerificationPurpose.Registration, 15);
            for (var i = 0; i < _emailVerificationSettings.RegistrationMaxAttempts; i++)
                verification.RegisterFailedAttempt();

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<LockedException>();
        }

        [Fact]
        public async Task Handle_ExceededMaxAttemptsButStillValid_DoesNotSendNewCode()
        {
            // Arrange
            var user = CreateUnconfirmedUser();
            var command = CreateCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "code_hash", EmailVerificationPurpose.Registration, 15);
            for (var i = 0; i < _emailVerificationSettings.RegistrationMaxAttempts; i++)
                verification.RegisterFailedAttempt();

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<LockedException>();

            // Assert
            _emailServiceMock.Verify(x => x.SendVerificationCodeAsync(
                It.IsAny<string>(), It.IsAny<EmailVerificationPurpose>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ── Expired → reissue ─────────────────────────────────────────────

        [Fact]
        public async Task Handle_ExpiredVerification_ReissuesNewCodeAndReturnsSuccess()
        {
            // Arrange
            var user = CreateUnconfirmedUser();
            var command = CreateCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "old_hash", EmailVerificationPurpose.Registration, -1);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.Registration, It.IsAny<CancellationToken>()))
                .ReturnsAsync("new_hash");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ExpiredVerification_RevokesOldVerification()
        {
            // Arrange
            var user = CreateUnconfirmedUser();
            var command = CreateCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "old_hash", EmailVerificationPurpose.Registration, -1);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.Registration, It.IsAny<CancellationToken>()))
                .ReturnsAsync("new_hash");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _emailVerificationRepositoryMock.Verify(x => x.Update(verification), Times.Once);
        }

        [Fact]
        public async Task Handle_ExpiredVerification_SendsNewCodeToUserEmail()
        {
            // Arrange
            var user = CreateUnconfirmedUser();
            var command = CreateCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "old_hash", EmailVerificationPurpose.Registration, -1);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.Registration, It.IsAny<CancellationToken>()))
                .ReturnsAsync("new_hash");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _emailServiceMock.Verify(x => x.SendVerificationCodeAsync(
                user.Email, EmailVerificationPurpose.Registration, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ExpiredVerification_AddsNewVerificationWithFreshExpiry()
        {
            // Arrange
            var user = CreateUnconfirmedUser();
            var command = CreateCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "old_hash", EmailVerificationPurpose.Registration, -1);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.Registration, It.IsAny<CancellationToken>()))
                .ReturnsAsync("new_hash");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            _emailVerificationRepositoryMock.Verify(x => x.AddAsync(It.Is<EmailVerification>(v =>
                v.UserId == user.Id &&
                v.Purpose == EmailVerificationPurpose.Registration &&
                v.CodeHash == "new_hash" &&
                v.IsValid()), It.IsAny<CancellationToken>()), Times.Once);

            result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        }

        [Fact]
        public async Task Handle_ExpiredVerification_SavesChanges()
        {
            // Arrange
            var user = CreateUnconfirmedUser();
            var command = CreateCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "old_hash", EmailVerificationPurpose.Registration, -1);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.Registration, It.IsAny<CancellationToken>()))
                .ReturnsAsync("new_hash");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}