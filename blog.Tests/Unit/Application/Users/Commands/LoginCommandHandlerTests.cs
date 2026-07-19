using blog.Application.Users.Commands.Login;
using blog.Domain.Common.Interfaces;
using blog.Domain.Common.Settings;
using blog.Domain.EmailVerifications.Entities;
using blog.Domain.EmailVerifications.Enums;
using blog.Domain.EmailVerifications.Repository;
using blog.Domain.Exceptions;
using blog.Domain.Tokens.Repository;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Repository;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace blog.Tests.Unit.Application.Users.Commands
{
    public class LoginCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
        private readonly Mock<IEmailVerificationRepository> _emailVerificationRepositoryMock = new();
        private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
        private readonly Mock<IHasher> _tokenHasherMock = new();
        private readonly Mock<IJwtService> _jwtServiceMock = new();
        private readonly Mock<IEmailService> _emailServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly LoginCommandHandler _handler;

        private readonly AccountLockoutSettings _lockoutSettings = new()
        {
            MaxFailedAttempts = 5,
            LockoutDurationMinutes = 15
        };

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

        public LoginCommandHandlerTests()
        {
            _handler = new LoginCommandHandler(
                _userRepositoryMock.Object,
                _refreshTokenRepositoryMock.Object,
                _emailVerificationRepositoryMock.Object,
                _passwordHasherMock.Object,
                _tokenHasherMock.Object,
                _jwtServiceMock.Object,
                _emailServiceMock.Object,
                Options.Create(_lockoutSettings),
                Options.Create(_emailVerificationSettings),
                _unitOfWorkMock.Object);
        }

        private static User CreateConfirmedUser(bool twoFactorEnabled = false)
        {
            var user = new User("test@test.com", "Ali", "Rezaei", "hashed_password");
            user.ConfirmEmail();

            if (twoFactorEnabled)
                user.EnableTwoFactor();

            return user;
        }

        private static LoginCommand CreateValidCommand(string email = "test@test.com") => new()
        {
            Email = email,
            Password = "Password123!",
            DeviceInfo = "test-device"
        };

        // ── User not found ──────────────────────────────────────────────

        [Fact]
        public async Task Handle_UserNotFound_ThrowsValidationException()
        {
            // Arrange
            var command = CreateValidCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task Handle_UserNotFound_StillHashesPasswordToPreventTimingAttack()
        {
            // Arrange
            var command = CreateValidCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<ValidationException>();

            // Assert
            _passwordHasherMock.Verify(x => x.Hash(command.Password), Times.Once);
        }

        // ── Lockout ──────────────────────────────────────────────────────

        [Fact]
        public async Task Handle_UserIsLockedOut_ThrowsLockedException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            user.RegisterFailedLoginAttempt(1, TimeSpan.FromMinutes(15));

            var command = CreateValidCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<LockedException>();
        }

        [Fact]
        public async Task Handle_UserIsLockedOut_DoesNotVerifyPassword()
        {
            // Arrange
            var user = CreateConfirmedUser();
            user.RegisterFailedLoginAttempt(1, TimeSpan.FromMinutes(15));

            var command = CreateValidCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<LockedException>();

            // Assert
            _passwordHasherMock.Verify(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        // ── Invalid password ────────────────────────────────────────────

        [Fact]
        public async Task Handle_InvalidPassword_ThrowsValidationException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateValidCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.Password, user.PasswordHash))
                .Returns(false);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task Handle_InvalidPassword_RegistersFailedLoginAttempt()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateValidCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.Password, user.PasswordHash))
                .Returns(false);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<ValidationException>();

            // Assert
            user.FailedLoginAttempts.Should().Be(1);
            _userRepositoryMock.Verify(x => x.Update(user), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ── Email not confirmed ─────────────────────────────────────────

        [Fact]
        public async Task Handle_EmailNotConfirmed_ThrowsEmailNotConfirmedException()
        {
            // Arrange
            var user = new User("test@test.com", "Ali", "Rezaei", "hashed_password");
            var command = CreateValidCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.Password, user.PasswordHash))
                .Returns(true);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<EmailNotConfirmedException>();
        }

        // ── Banned / deleted ─────────────────────────────────────────────

        [Fact]
        public async Task Handle_UserIsBanned_ThrowsInvalidStateException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            user.Ban();

            var command = CreateValidCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.Password, user.PasswordHash))
                .Returns(true);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        // ── Successful login, 2FA disabled ──────────────────────────────

        [Fact]
        public async Task Handle_ValidCredentialsAndTwoFactorDisabled_ReturnsTokens()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateValidCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.Password, user.PasswordHash))
                .Returns(true);

            _jwtServiceMock.Setup(x => x.GenerateAccessToken(user)).Returns("access_token");
            _jwtServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh_token");
            _tokenHasherMock.Setup(x => x.Hash("refresh_token")).Returns("hashed_refresh_token");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.RequiresTwoFactor.Should().BeFalse();
            result.AccessToken.Should().Be("access_token");
            result.RefreshToken.Should().Be("refresh_token");
            result.ChallengeId.Should().BeNull();
        }

        [Fact]
        public async Task Handle_ValidCredentialsAndTwoFactorDisabled_AddsRefreshToken()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateValidCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.Password, user.PasswordHash))
                .Returns(true);

            _jwtServiceMock.Setup(x => x.GenerateAccessToken(user)).Returns("access_token");
            _jwtServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh_token");
            _tokenHasherMock.Setup(x => x.Hash("refresh_token")).Returns("hashed_refresh_token");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _refreshTokenRepositoryMock.Verify(x => x.AddAsync(
                It.Is<Domain.Tokens.Entities.RefreshToken>(t => t.TokenHash == "hashed_refresh_token"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCredentialsAndTwoFactorDisabled_ResetsFailedLoginAttempts()
        {
            // Arrange
            var user = CreateConfirmedUser();
            user.RegisterFailedLoginAttempt(5, TimeSpan.FromMinutes(15));
            // simulate lockout expired
            typeof(User).GetProperty(nameof(User.LockedOutUntil))!.SetValue(user, DateTime.UtcNow.AddMinutes(-1));

            var command = CreateValidCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.Password, user.PasswordHash))
                .Returns(true);

            _jwtServiceMock.Setup(x => x.GenerateAccessToken(user)).Returns("access_token");
            _jwtServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh_token");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            user.FailedLoginAttempts.Should().Be(0);
        }

        [Fact]
        public async Task Handle_ValidCredentialsAndTwoFactorDisabled_DoesNotSendVerificationCode()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateValidCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.Password, user.PasswordHash))
                .Returns(true);

            _jwtServiceMock.Setup(x => x.GenerateAccessToken(user)).Returns("access_token");
            _jwtServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh_token");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _emailServiceMock.Verify(x => x.SendVerificationCodeAsync(
                It.IsAny<string>(), It.IsAny<EmailVerificationPurpose>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ── Successful login, 2FA enabled ───────────────────────────────

        [Fact]
        public async Task Handle_TwoFactorEnabled_ReturnsChallengeIdInsteadOfTokens()
        {
            // Arrange
            var user = CreateConfirmedUser(twoFactorEnabled: true);
            var command = CreateValidCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.Password, user.PasswordHash))
                .Returns(true);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((EmailVerification?)null);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.LoginVerification, It.IsAny<CancellationToken>()))
                .ReturnsAsync("hashed_code");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.RequiresTwoFactor.Should().BeTrue();
            result.ChallengeId.Should().NotBeNull();
            result.AccessToken.Should().BeNull();
            result.RefreshToken.Should().BeNull();
        }

        [Fact]
        public async Task Handle_TwoFactorEnabled_SendsVerificationCodeWithLoginVerificationPurpose()
        {
            // Arrange
            var user = CreateConfirmedUser(twoFactorEnabled: true);
            var command = CreateValidCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.Password, user.PasswordHash))
                .Returns(true);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((EmailVerification?)null);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.LoginVerification, It.IsAny<CancellationToken>()))
                .ReturnsAsync("hashed_code");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _emailServiceMock.Verify(x => x.SendVerificationCodeAsync(
                user.Email, EmailVerificationPurpose.LoginVerification, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_TwoFactorEnabled_AddsEmailVerificationWithCorrectData()
        {
            // Arrange
            var user = CreateConfirmedUser(twoFactorEnabled: true);
            var command = CreateValidCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.Password, user.PasswordHash))
                .Returns(true);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((EmailVerification?)null);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.LoginVerification, It.IsAny<CancellationToken>()))
                .ReturnsAsync("hashed_code");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _emailVerificationRepositoryMock.Verify(x => x.AddAsync(It.Is<EmailVerification>(v =>
                v.UserId == user.Id &&
                v.Purpose == EmailVerificationPurpose.LoginVerification &&
                v.CodeHash == "hashed_code" &&
                v.TargetEmail == null), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_TwoFactorEnabled_DoesNotAddRefreshToken()
        {
            // Arrange
            var user = CreateConfirmedUser(twoFactorEnabled: true);
            var command = CreateValidCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.Password, user.PasswordHash))
                .Returns(true);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((EmailVerification?)null);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.LoginVerification, It.IsAny<CancellationToken>()))
                .ReturnsAsync("hashed_code");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _refreshTokenRepositoryMock.Verify(x => x.AddAsync(
                It.IsAny<Domain.Tokens.Entities.RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
            _jwtServiceMock.Verify(x => x.GenerateAccessToken(It.IsAny<User>()), Times.Never);
        }

        // ── Already-active LoginVerification ─────────────────────────────

        [Fact]
        public async Task Handle_TwoFactorEnabledWithActiveLoginVerification_ThrowsAlreadyExistsException()
        {
            // Arrange
            var user = CreateConfirmedUser(twoFactorEnabled: true);
            var command = CreateValidCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.Password, user.PasswordHash))
                .Returns(true);

            var existingVerification = new EmailVerification(user.Id, "existing_hash", EmailVerificationPurpose.LoginVerification, 10);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingVerification);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<AlreadyExistsException>();
        }

        [Fact]
        public async Task Handle_TwoFactorEnabledWithActiveLoginVerification_DoesNotSendNewCode()
        {
            // Arrange
            var user = CreateConfirmedUser(twoFactorEnabled: true);
            var command = CreateValidCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.Password, user.PasswordHash))
                .Returns(true);

            var existingVerification = new EmailVerification(user.Id, "existing_hash", EmailVerificationPurpose.LoginVerification, 10);

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingVerification);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<AlreadyExistsException>();

            // Assert
            _emailServiceMock.Verify(x => x.SendVerificationCodeAsync(
                It.IsAny<string>(), It.IsAny<EmailVerificationPurpose>(), It.IsAny<CancellationToken>()), Times.Never);
            _emailVerificationRepositoryMock.Verify(x => x.AddAsync(
                It.IsAny<EmailVerification>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_TwoFactorEnabledWithActiveDifferentPurposeVerification_StillIssuesNewLoginCode()
        {
            // Arrange
            var user = CreateConfirmedUser(twoFactorEnabled: true);
            var command = CreateValidCommand();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.Password, user.PasswordHash))
                .Returns(true);

            var existingVerification = new EmailVerification(user.Id, "existing_hash", EmailVerificationPurpose.ChangeEmail, 15, "new@test.com");

            _emailVerificationRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingVerification);

            _emailServiceMock
                .Setup(x => x.SendVerificationCodeAsync(user.Email, EmailVerificationPurpose.LoginVerification, It.IsAny<CancellationToken>()))
                .ReturnsAsync("hashed_code");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.RequiresTwoFactor.Should().BeTrue();
            _emailVerificationRepositoryMock.Verify(x => x.AddAsync(
                It.Is<EmailVerification>(v => v.Purpose == EmailVerificationPurpose.LoginVerification),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}