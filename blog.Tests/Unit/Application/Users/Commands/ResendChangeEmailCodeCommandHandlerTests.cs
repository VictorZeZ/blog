using blog.Application.Users.Commands.ResendChangeEmailCode;
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
    public class ResendChangeEmailCodeCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IEmailVerificationRepository> _emailVerificationRepositoryMock = new();
        private readonly Mock<IEmailService> _emailServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly ResendChangeEmailCodeCommandHandler _handler;

        private const string TargetEmail = "new@test.com";

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
            ConfirmNewEmailExpiryMinutes = 20,
            ConfirmNewEmailMaxAttempts = 5
        };

        public ResendChangeEmailCodeCommandHandlerTests()
        {
            _handler = new ResendChangeEmailCodeCommandHandler(
                _userRepositoryMock.Object,
                _emailVerificationRepositoryMock.Object,
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

        private static ResendChangeEmailCodeCommand CreateCommand(Guid userId) => new()
        {
            UserId = userId
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

        // ── Missing / unrelated-purpose verification ────────────────────

        [Fact]
        public async Task Handle_NoActiveVerification_ThrowsNotFoundException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
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
        public async Task Handle_ActiveVerificationHasUnrelatedPurpose_ThrowsNotFoundException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
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
        public async Task Handle_ChangeEmailStepStillValid_ReturnsExistingExpiresAtWithoutSendingNewCode()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "code_hash", EmailVerificationPurpose.ChangeEmail, 15, TargetEmail);

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
        }

        // ── Exceeded attempts, still valid → locked ─────────────────────

        [Fact]
        public async Task Handle_ExceededMaxAttemptsButStillValid_ThrowsLockedException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "code_hash", EmailVerificationPurpose.ChangeEmail, 15, TargetEmail);
            for (var i = 0; i < _emailVerificationSettings.ChangeEmailMaxAttempts; i++)
                verification.RegisterFailedAttempt();

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<LockedException>();
        }

        // ── Expired: ChangeEmail step → resend to CURRENT email ─────────

        [Fact]
        public async Task Handle_ChangeEmailStepExpired_SendsNewCodeToCurrentUserEmail()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "old_hash", EmailVerificationPurpose.ChangeEmail, -1, TargetEmail);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.ChangeEmail, It.IsAny<CancellationToken>()))
                .ReturnsAsync("new_hash");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            _emailServiceMock.Verify(x => x.SendVerificationCodeAsync(
                user.Email, EmailVerificationPurpose.ChangeEmail, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ChangeEmailStepExpired_AddsNewVerificationPreservingTargetEmailAndPurpose()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "old_hash", EmailVerificationPurpose.ChangeEmail, -1, TargetEmail);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.ChangeEmail, It.IsAny<CancellationToken>()))
                .ReturnsAsync("new_hash");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _emailVerificationRepositoryMock.Verify(x => x.AddAsync(It.Is<EmailVerification>(v =>
                v.Purpose == EmailVerificationPurpose.ChangeEmail &&
                v.TargetEmail == TargetEmail &&
                v.CodeHash == "new_hash" &&
                v.IsValid()), It.IsAny<CancellationToken>()), Times.Once);
        }

        // ── Expired: ConfirmNewEmail step → resend to NEW email ──────────

        [Fact]
        public async Task Handle_ConfirmNewEmailStepExpired_SendsNewCodeToTargetEmail()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "old_hash", EmailVerificationPurpose.ConfirmNewEmail, -1, TargetEmail);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(TargetEmail, EmailVerificationPurpose.ConfirmNewEmail, It.IsAny<CancellationToken>()))
                .ReturnsAsync("new_hash");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            _emailServiceMock.Verify(x => x.SendVerificationCodeAsync(
                TargetEmail, EmailVerificationPurpose.ConfirmNewEmail, It.IsAny<CancellationToken>()), Times.Once);

            // Must never send to the user's current/old email for this step
            _emailServiceMock.Verify(x => x.SendVerificationCodeAsync(
                user.Email, It.IsAny<EmailVerificationPurpose>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ConfirmNewEmailStepExpired_AddsNewVerificationPreservingTargetEmailAndPurpose()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "old_hash", EmailVerificationPurpose.ConfirmNewEmail, -1, TargetEmail);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(TargetEmail, EmailVerificationPurpose.ConfirmNewEmail, It.IsAny<CancellationToken>()))
                .ReturnsAsync("new_hash");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _emailVerificationRepositoryMock.Verify(x => x.AddAsync(It.Is<EmailVerification>(v =>
                v.Purpose == EmailVerificationPurpose.ConfirmNewEmail &&
                v.TargetEmail == TargetEmail &&
                v.CodeHash == "new_hash" &&
                v.IsValid()), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ExpiredVerification_RevokesOldVerification()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "old_hash", EmailVerificationPurpose.ChangeEmail, -1, TargetEmail);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.ChangeEmail, It.IsAny<CancellationToken>()))
                .ReturnsAsync("new_hash");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _emailVerificationRepositoryMock.Verify(x => x.Update(verification), Times.Once);
        }

        [Fact]
        public async Task Handle_ExpiredVerification_SavesChanges()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "old_hash", EmailVerificationPurpose.ChangeEmail, -1, TargetEmail);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.ChangeEmail, It.IsAny<CancellationToken>()))
                .ReturnsAsync("new_hash");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}