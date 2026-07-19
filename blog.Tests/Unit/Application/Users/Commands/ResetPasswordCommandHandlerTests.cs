using blog.Application.Users.Commands.ResetPassword;
using blog.Domain.Common.Interfaces;
using blog.Domain.Common.Settings;
using blog.Domain.EmailVerifications.Entities;
using blog.Domain.EmailVerifications.Enums;
using blog.Domain.EmailVerifications.Repository;
using blog.Domain.Exceptions;
using blog.Domain.Tokens.Entities;
using blog.Domain.Tokens.Repository;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Repository;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace blog.Tests.Unit.Application.Users.Commands
{
    public class ResetPasswordCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IEmailVerificationRepository> _emailVerificationRepositoryMock = new();
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
        private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
        private readonly Mock<IHasher> _hasherMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly ResetPasswordCommandHandler _handler;

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

        public ResetPasswordCommandHandlerTests()
        {
            _handler = new ResetPasswordCommandHandler(
                _userRepositoryMock.Object,
                _emailVerificationRepositoryMock.Object,
                _refreshTokenRepositoryMock.Object,
                _passwordHasherMock.Object,
                _hasherMock.Object,
                Options.Create(_emailVerificationSettings),
                _unitOfWorkMock.Object);
        }

        private static User CreateConfirmedUser()
        {
            var user = new User("test@test.com", "Ali", "Rezaei", "hashed_password");
            user.ConfirmEmail();
            return user;
        }

        private static ResetPasswordCommand CreateCommand(string email = "test@test.com") => new()
        {
            Email = email,
            Code = "ABC123",
            NewPassword = "N3wStr0ng!Pass"
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

        // ── Missing / wrong-purpose verification ────────────────────────

        [Fact]
        public async Task Handle_NoActiveVerification_ThrowsNotFoundException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Email);

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
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Email);

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "code_hash", EmailVerificationPurpose.LoginVerification, 10);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        // ── Expired ──────────────────────────────────────────────────────

        [Fact]
        public async Task Handle_ExpiredVerification_ThrowsExpiredException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Email);

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "code_hash", EmailVerificationPurpose.ResetPassword, -1);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
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
            var command = CreateCommand(user.Email);

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "correct_hash", EmailVerificationPurpose.ResetPassword, 15);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
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
            var command = CreateCommand(user.Email);

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "correct_hash", EmailVerificationPurpose.ResetPassword, 15);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
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
        public async Task Handle_InvalidCode_DoesNotChangePassword()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var originalPasswordHash = user.PasswordHash;
            var command = CreateCommand(user.Email);

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "correct_hash", EmailVerificationPurpose.ResetPassword, 15);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _hasherMock
                .Setup(x => x.Hash(command.Code.ToUpper()))
                .Returns("wrong_hash");

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<ValidationException>();

            // Assert
            user.PasswordHash.Should().Be(originalPasswordHash);
            _passwordHasherMock.Verify(x => x.Hash(It.IsAny<string>()), Times.Never);
            _refreshTokenRepositoryMock.Verify(x => x.GetActiveByUserIdAsync(It.IsAny<Domain.Users.Types.UserId>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ── Successful reset ──────────────────────────────────────────────

        [Fact]
        public async Task Handle_ValidCode_ReturnsSuccessResponse()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Email);

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "correct_hash", EmailVerificationPurpose.ResetPassword, 15);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _hasherMock.Setup(x => x.Hash(command.Code.ToUpper())).Returns("correct_hash");
            _passwordHasherMock.Setup(x => x.Hash(command.NewPassword)).Returns("new_hashed_password");

            _refreshTokenRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ValidCode_ChangesUserPassword()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Email);

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "correct_hash", EmailVerificationPurpose.ResetPassword, 15);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _hasherMock.Setup(x => x.Hash(command.Code.ToUpper())).Returns("correct_hash");
            _passwordHasherMock.Setup(x => x.Hash(command.NewPassword)).Returns("new_hashed_password");

            _refreshTokenRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            user.PasswordHash.Should().Be("new_hashed_password");
            _userRepositoryMock.Verify(x => x.Update(user), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCode_MarksVerificationAsVerified()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Email);

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "correct_hash", EmailVerificationPurpose.ResetPassword, 15);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _hasherMock.Setup(x => x.Hash(command.Code.ToUpper())).Returns("correct_hash");
            _passwordHasherMock.Setup(x => x.Hash(command.NewPassword)).Returns("new_hashed_password");

            _refreshTokenRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            verification.IsValid().Should().BeFalse();
            _emailVerificationRepositoryMock.Verify(x => x.Update(verification), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCode_RevokesAllActiveRefreshTokens()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Email);

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "correct_hash", EmailVerificationPurpose.ResetPassword, 15);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _hasherMock.Setup(x => x.Hash(command.Code.ToUpper())).Returns("correct_hash");
            _passwordHasherMock.Setup(x => x.Hash(command.NewPassword)).Returns("new_hashed_password");

            var activeToken1 = new RefreshToken("hash1", user.Id, "device1");
            var activeToken2 = new RefreshToken("hash2", user.Id, "device2");

            _refreshTokenRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync([activeToken1, activeToken2]);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            activeToken1.IsValid().Should().BeFalse();
            activeToken2.IsValid().Should().BeFalse();
            _refreshTokenRepositoryMock.Verify(x => x.Update(activeToken1), Times.Once);
            _refreshTokenRepositoryMock.Verify(x => x.Update(activeToken2), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCode_SavesChanges()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Email);

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var verification = new EmailVerification(user.Id, "correct_hash", EmailVerificationPurpose.ResetPassword, 15);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(verification);

            _hasherMock.Setup(x => x.Hash(command.Code.ToUpper())).Returns("correct_hash");
            _passwordHasherMock.Setup(x => x.Hash(command.NewPassword)).Returns("new_hashed_password");

            _refreshTokenRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}