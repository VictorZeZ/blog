using blog.Application.Users.Commands.ResendLoginVerificationCode;
using blog.Domain.Common.Interfaces;
using blog.Domain.Common.Settings;
using blog.Domain.EmailVerifications.Entities;
using blog.Domain.EmailVerifications.Enums;
using blog.Domain.EmailVerifications.Repository;
using blog.Domain.EmailVerifications.Types;
using blog.Domain.Exceptions;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Repository;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace blog.Tests.Unit.Application.Users.Commands
{
    public class ResendLoginVerificationCodeCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IEmailVerificationRepository> _emailVerificationRepositoryMock = new();
        private readonly Mock<IEmailService> _emailServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly ResendLoginVerificationCodeCommandHandler _handler;

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

        public ResendLoginVerificationCodeCommandHandlerTests()
        {
            _handler = new ResendLoginVerificationCodeCommandHandler(
                _userRepositoryMock.Object,
                _emailVerificationRepositoryMock.Object,
                _emailServiceMock.Object,
                Options.Create(_emailVerificationSettings),
                _unitOfWorkMock.Object);
        }

        private static User CreateConfirmedUser()
        {
            var user = new User("test@test.com", "Ali", "Rezaei", "hashed_password");
            user.ConfirmEmail();
            user.EnableTwoFactor();
            return user;
        }

        private static ResendLoginVerificationCodeCommand CreateCommand(Guid challengeId) => new()
        {
            ChallengeId = challengeId
        };

        // ── Missing / wrong-purpose challenge ────────────────────────────

        [Fact]
        public async Task Handle_ChallengeNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var challengeId = Guid.NewGuid();
            var command = CreateCommand(challengeId);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetByIdAsync(new EmailVerificationId(challengeId), It.IsAny<CancellationToken>()))
                .ReturnsAsync((EmailVerification?)null);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_ChallengeHasDifferentPurpose_ThrowsNotFoundException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var verification = new EmailVerification(user.Id, "code_hash", EmailVerificationPurpose.Registration, 15);
            var command = CreateCommand(verification.Id.Value);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetByIdAsync(verification.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        // ── User not found ──────────────────────────────────────────────

        [Fact]
        public async Task Handle_UserNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var missingUserId = new blog.Domain.Users.Types.UserId(Guid.NewGuid());
            var verification = new EmailVerification(missingUserId, "code_hash", EmailVerificationPurpose.LoginVerification, 10);
            var command = CreateCommand(verification.Id.Value);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetByIdAsync(verification.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(missingUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        // ── User banned between login attempt and resend ─────────────────

        [Fact]
        public async Task Handle_UserIsBanned_ThrowsInvalidStateException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            user.Ban();

            var verification = new EmailVerification(user.Id, "code_hash", EmailVerificationPurpose.LoginVerification, 10);
            var command = CreateCommand(verification.Id.Value);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetByIdAsync(verification.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        // ── Still valid, not exceeded → no reissue, same ChallengeId ────

        [Fact]
        public async Task Handle_StillValidAndNotExceeded_ReturnsSameChallengeIdAndExistingExpiresAt()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var verification = new EmailVerification(user.Id, "code_hash", EmailVerificationPurpose.LoginVerification, 10);
            var command = CreateCommand(verification.Id.Value);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetByIdAsync(verification.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.ChallengeId.Should().Be(verification.Id.Value);
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
            var verification = new EmailVerification(user.Id, "code_hash", EmailVerificationPurpose.LoginVerification, 10);
            for (var i = 0; i < _emailVerificationSettings.LoginVerificationMaxAttempts; i++)
                verification.RegisterFailedAttempt();

            var command = CreateCommand(verification.Id.Value);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetByIdAsync(verification.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<LockedException>();
        }

        // ── Expired → reissue with new ChallengeId ───────────────────────

        [Fact]
        public async Task Handle_ExpiredVerification_ReturnsNewChallengeIdDifferentFromOld()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var verification = new EmailVerification(user.Id, "old_hash", EmailVerificationPurpose.LoginVerification, -1);
            var command = CreateCommand(verification.Id.Value);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetByIdAsync(verification.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.LoginVerification, It.IsAny<CancellationToken>()))
                .ReturnsAsync("new_hash");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
            result.ChallengeId.Should().NotBe(verification.Id.Value);
        }

        [Fact]
        public async Task Handle_ExpiredVerification_RevokesOldVerification()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var verification = new EmailVerification(user.Id, "old_hash", EmailVerificationPurpose.LoginVerification, -1);
            var command = CreateCommand(verification.Id.Value);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetByIdAsync(verification.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.LoginVerification, It.IsAny<CancellationToken>()))
                .ReturnsAsync("new_hash");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _emailVerificationRepositoryMock.Verify(x => x.Update(verification), Times.Once);
        }

        [Fact]
        public async Task Handle_ExpiredVerification_AddsNewVerificationWithSameUserAndPurpose()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var verification = new EmailVerification(user.Id, "old_hash", EmailVerificationPurpose.LoginVerification, -1);
            var command = CreateCommand(verification.Id.Value);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetByIdAsync(verification.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.LoginVerification, It.IsAny<CancellationToken>()))
                .ReturnsAsync("new_hash");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _emailVerificationRepositoryMock.Verify(x => x.AddAsync(It.Is<EmailVerification>(v =>
                v.UserId == user.Id &&
                v.Purpose == EmailVerificationPurpose.LoginVerification &&
                v.CodeHash == "new_hash" &&
                v.IsValid()), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ExpiredVerification_SavesChanges()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var verification = new EmailVerification(user.Id, "old_hash", EmailVerificationPurpose.LoginVerification, -1);
            var command = CreateCommand(verification.Id.Value);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetByIdAsync(verification.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.LoginVerification, It.IsAny<CancellationToken>()))
                .ReturnsAsync("new_hash");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}