using blog.Application.Users.Commands.ConfirmLogin;
using blog.Domain.Common.Interfaces;
using blog.Domain.Common.Settings;
using blog.Domain.EmailVerifications.Entities;
using blog.Domain.EmailVerifications.Enums;
using blog.Domain.EmailVerifications.Repository;
using blog.Domain.EmailVerifications.Types;
using blog.Domain.Exceptions;
using blog.Domain.Tokens.Repository;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Repository;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace blog.Tests.Unit.Application.Users.Commands
{
    public class ConfirmLoginCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IEmailVerificationRepository> _emailVerificationRepositoryMock = new();
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
        private readonly Mock<IHasher> _hasherMock = new();
        private readonly Mock<IJwtService> _jwtServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly ConfirmLoginCommandHandler _handler;

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

        public ConfirmLoginCommandHandlerTests()
        {
            _handler = new ConfirmLoginCommandHandler(
                _userRepositoryMock.Object,
                _emailVerificationRepositoryMock.Object,
                _refreshTokenRepositoryMock.Object,
                _hasherMock.Object,
                _jwtServiceMock.Object,
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

        private static ConfirmLoginCommand CreateValidCommand(Guid challengeId) => new()
        {
            ChallengeId = challengeId,
            Code = "ABC123",
            DeviceInfo = "test-device"
        };

        // ── Missing / wrong-purpose challenge ────────────────────────────

        [Fact]
        public async Task Handle_ChallengeNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var challengeId = Guid.NewGuid();
            var command = CreateValidCommand(challengeId);

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
            var command = CreateValidCommand(verification.Id.Value);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetByIdAsync(verification.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        // ── Expired ──────────────────────────────────────────────────────

        [Fact]
        public async Task Handle_ExpiredChallenge_ThrowsExpiredException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var verification = new EmailVerification(user.Id, "code_hash", EmailVerificationPurpose.LoginVerification, -1);
            var command = CreateValidCommand(verification.Id.Value);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetByIdAsync(verification.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ExpiredException>();
        }

        // ── Exceeded attempts ────────────────────────────────────────────

        [Fact]
        public async Task Handle_ExceededMaxAttempts_ThrowsLockedException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var verification = new EmailVerification(user.Id, "code_hash", EmailVerificationPurpose.LoginVerification, 10);
            for (var i = 0; i < _emailVerificationSettings.LoginVerificationMaxAttempts; i++)
                verification.RegisterFailedAttempt();

            var command = CreateValidCommand(verification.Id.Value);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetByIdAsync(verification.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<LockedException>();
        }

        // ── Invalid code ─────────────────────────────────────────────────

        [Fact]
        public async Task Handle_InvalidCode_ThrowsValidationException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var verification = new EmailVerification(user.Id, "correct_hash", EmailVerificationPurpose.LoginVerification, 10);
            var command = CreateValidCommand(verification.Id.Value);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetByIdAsync(verification.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _hasherMock
                .Setup(x => x.Hash(command.Code.ToUpper()))
                .Returns("wrong_hash");

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task Handle_InvalidCode_RegistersFailedAttemptAndSavesChanges()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var verification = new EmailVerification(user.Id, "correct_hash", EmailVerificationPurpose.LoginVerification, 10);
            var command = CreateValidCommand(verification.Id.Value);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetByIdAsync(verification.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _hasherMock
                .Setup(x => x.Hash(command.Code.ToUpper()))
                .Returns("wrong_hash");

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<ValidationException>();

            // Assert
            verification.AttemptCount.Should().Be(1);
            _emailVerificationRepositoryMock.Verify(x => x.Update(verification), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_InvalidCode_DoesNotIssueTokens()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var verification = new EmailVerification(user.Id, "correct_hash", EmailVerificationPurpose.LoginVerification, 10);
            var command = CreateValidCommand(verification.Id.Value);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetByIdAsync(verification.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _hasherMock
                .Setup(x => x.Hash(command.Code.ToUpper()))
                .Returns("wrong_hash");

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<ValidationException>();

            // Assert
            _jwtServiceMock.Verify(x => x.GenerateAccessToken(It.IsAny<User>()), Times.Never);
            _refreshTokenRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Domain.Tokens.Entities.RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ── User not found (edge case: user deleted between login and confirm) ──

        [Fact]
        public async Task Handle_UserNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var missingUserId = new blog.Domain.Users.Types.UserId(Guid.NewGuid());
            var verification = new EmailVerification(missingUserId, "correct_hash", EmailVerificationPurpose.LoginVerification, 10);
            var command = CreateValidCommand(verification.Id.Value);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetByIdAsync(verification.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _hasherMock
                .Setup(x => x.Hash(command.Code.ToUpper()))
                .Returns("correct_hash");

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(missingUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        // ── User banned/deleted between login attempt and confirm ───────

        [Fact]
        public async Task Handle_UserIsBanned_ThrowsInvalidStateException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            user.Ban();

            var verification = new EmailVerification(user.Id, "correct_hash", EmailVerificationPurpose.LoginVerification, 10);
            var command = CreateValidCommand(verification.Id.Value);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetByIdAsync(verification.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _hasherMock
                .Setup(x => x.Hash(command.Code.ToUpper()))
                .Returns("correct_hash");

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        // ── Successful confirmation ──────────────────────────────────────

        [Fact]
        public async Task Handle_ValidCode_ReturnsTokens()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var verification = new EmailVerification(user.Id, "correct_hash", EmailVerificationPurpose.LoginVerification, 10);
            var command = CreateValidCommand(verification.Id.Value);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetByIdAsync(verification.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _hasherMock.Setup(x => x.Hash(command.Code.ToUpper())).Returns("correct_hash");
            _hasherMock.Setup(x => x.Hash("refresh_token")).Returns("hashed_refresh_token");

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _jwtServiceMock.Setup(x => x.GenerateAccessToken(user)).Returns("access_token");
            _jwtServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh_token");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.AccessToken.Should().Be("access_token");
            result.RefreshToken.Should().Be("refresh_token");
        }

        [Fact]
        public async Task Handle_ValidCode_MarksVerificationAsVerified()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var verification = new EmailVerification(user.Id, "correct_hash", EmailVerificationPurpose.LoginVerification, 10);
            var command = CreateValidCommand(verification.Id.Value);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetByIdAsync(verification.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _hasherMock.Setup(x => x.Hash(command.Code.ToUpper())).Returns("correct_hash");
            _hasherMock.Setup(x => x.Hash("refresh_token")).Returns("hashed_refresh_token");

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _jwtServiceMock.Setup(x => x.GenerateAccessToken(user)).Returns("access_token");
            _jwtServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh_token");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            verification.IsValid().Should().BeFalse();
            _emailVerificationRepositoryMock.Verify(x => x.Update(verification), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCode_AddsRefreshTokenWithHashedValue()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var verification = new EmailVerification(user.Id, "correct_hash", EmailVerificationPurpose.LoginVerification, 10);
            var command = CreateValidCommand(verification.Id.Value);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetByIdAsync(verification.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _hasherMock.Setup(x => x.Hash(command.Code.ToUpper())).Returns("correct_hash");
            _hasherMock.Setup(x => x.Hash("refresh_token")).Returns("hashed_refresh_token");

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _jwtServiceMock.Setup(x => x.GenerateAccessToken(user)).Returns("access_token");
            _jwtServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh_token");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _refreshTokenRepositoryMock.Verify(x => x.AddAsync(
                It.Is<Domain.Tokens.Entities.RefreshToken>(t =>
                    t.TokenHash == "hashed_refresh_token" &&
                    t.UserId == user.Id &&
                    t.DeviceInfo == command.DeviceInfo),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCode_SavesChanges()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var verification = new EmailVerification(user.Id, "correct_hash", EmailVerificationPurpose.LoginVerification, 10);
            var command = CreateValidCommand(verification.Id.Value);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetByIdAsync(verification.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _hasherMock.Setup(x => x.Hash(command.Code.ToUpper())).Returns("correct_hash");
            _hasherMock.Setup(x => x.Hash("refresh_token")).Returns("hashed_refresh_token");

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _jwtServiceMock.Setup(x => x.GenerateAccessToken(user)).Returns("access_token");
            _jwtServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh_token");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}