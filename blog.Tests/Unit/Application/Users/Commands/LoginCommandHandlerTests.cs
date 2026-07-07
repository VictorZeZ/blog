using blog.Application.Users.Commands.Login;
using blog.Domain.Common.Interfaces;
using blog.Domain.Common.Settings;
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
    public class LoginCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
        private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
        private readonly Mock<ITokenHasher> _tokenHasherMock = new();
        private readonly Mock<IJwtService> _jwtServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly LoginCommandHandler _handler;

        private static AccountLockoutSettings LockoutSettings => new()
        {
            MaxFailedAttempts = 3,
            LockoutDurationMinutes = 15
        };

        public LoginCommandHandlerTests()
        {
            _handler = new LoginCommandHandler(
                _userRepositoryMock.Object,
                _refreshTokenRepositoryMock.Object,
                _passwordHasherMock.Object,
                _tokenHasherMock.Object,
                _jwtServiceMock.Object,
                Options.Create(LockoutSettings),
                _unitOfWorkMock.Object);
        }

        private static LoginCommand ValidCommand => new()
        {
            Email = "test@test.com",
            Password = "Password123",
            DeviceInfo = "Mozilla/5.0 Chrome/120"
        };

        private static User ValidUser => new(
            "test@test.com",
            "Ali",
            "Rezaei",
            "hashed_password");

        [Fact]
        public async Task Handle_ValidCommand_ReturnsLoginResponse()
        {
            var command = ValidCommand;
            var user = ValidUser;

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.Password, user.PasswordHash))
                .Returns(true);

            _jwtServiceMock
                .Setup(x => x.GenerateAccessToken(user))
                .Returns("access_token");

            _jwtServiceMock
                .Setup(x => x.GenerateRefreshToken())
                .Returns("refresh_token");

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
            result.AccessToken.Should().Be("access_token");
            result.RefreshToken.Should().Be("refresh_token");
        }

        [Fact]
        public async Task Handle_UserNotFound_ThrowsValidationException()
        {
            var command = ValidCommand;

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            var act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task Handle_UserNotFound_StillPerformsDummyHashToPreventTimingLeak()
        {
            var command = ValidCommand;

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            var act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<ValidationException>();

            _passwordHasherMock.Verify(x => x.Hash(command.Password), Times.Once);
        }

        [Fact]
        public async Task Handle_InvalidPassword_ThrowsValidationException()
        {
            var command = ValidCommand;
            var user = ValidUser;

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.Password, user.PasswordHash))
                .Returns(false);

            var act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task Handle_InvalidPassword_IncrementsFailedLoginAttempts()
        {
            var command = ValidCommand;
            var user = ValidUser;

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.Password, user.PasswordHash))
                .Returns(false);

            var act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<ValidationException>();

            user.FailedLoginAttempts.Should().Be(1);
            _userRepositoryMock.Verify(x => x.Update(user), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_InvalidPassword_ReachingMaxAttempts_LocksAccount()
        {
            var command = ValidCommand;
            var user = ValidUser;

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.Password, user.PasswordHash))
                .Returns(false);

            for (var i = 0; i < LockoutSettings.MaxFailedAttempts; i++)
            {
                var act = () => _handler.Handle(command, CancellationToken.None);
                await act.Should().ThrowAsync<ValidationException>();
            }

            user.IsLockedOut().Should().BeTrue();
        }

        [Fact]
        public async Task Handle_LockedOutUser_ThrowsLockedExceptionWithoutCheckingPassword()
        {
            var command = ValidCommand;
            var user = ValidUser;

            for (var i = 0; i < LockoutSettings.MaxFailedAttempts; i++)
                user.RegisterFailedLoginAttempt(LockoutSettings.MaxFailedAttempts, TimeSpan.FromMinutes(LockoutSettings.LockoutDurationMinutes));

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<LockedException>();
            _passwordHasherMock.Verify(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ExpiredLockout_AllowsLoginAttempt()
        {
            var command = ValidCommand;
            var user = ValidUser;

            for (var i = 0; i < LockoutSettings.MaxFailedAttempts; i++)
                user.RegisterFailedLoginAttempt(LockoutSettings.MaxFailedAttempts, TimeSpan.FromMinutes(-1));

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.Password, user.PasswordHash))
                .Returns(true);

            _jwtServiceMock
                .Setup(x => x.GenerateAccessToken(user))
                .Returns("access_token");

            _jwtServiceMock
                .Setup(x => x.GenerateRefreshToken())
                .Returns("refresh_token");

            var result = await _handler.Handle(command, CancellationToken.None);

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_ValidCommand_ResetsFailedLoginAttempts()
        {
            var command = ValidCommand;
            var user = ValidUser;
            user.RegisterFailedLoginAttempt(LockoutSettings.MaxFailedAttempts, TimeSpan.FromMinutes(LockoutSettings.LockoutDurationMinutes));

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.Password, user.PasswordHash))
                .Returns(true);

            _jwtServiceMock
                .Setup(x => x.GenerateAccessToken(user))
                .Returns("access_token");

            _jwtServiceMock
                .Setup(x => x.GenerateRefreshToken())
                .Returns("refresh_token");

            await _handler.Handle(command, CancellationToken.None);

            user.FailedLoginAttempts.Should().Be(0);
        }

        [Fact]
        public async Task Handle_BannedUser_WithCorrectPassword_ThrowsInvalidStateException()
        {
            var command = ValidCommand;
            var user = ValidUser;
            user.Ban();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.Password, user.PasswordHash))
                .Returns(true);

            var act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_BannedUser_WithWrongPassword_ThrowsValidationExceptionNotAccountState()
        {
            var command = ValidCommand;
            var user = ValidUser;
            user.Ban();

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.Password, user.PasswordHash))
                .Returns(false);

            var act = () => _handler.Handle(command, CancellationToken.None);

            await act.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task Handle_ValidCommand_AddsRefreshToken()
        {
            var command = ValidCommand;
            var user = ValidUser;

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.Password, user.PasswordHash))
                .Returns(true);

            _jwtServiceMock
                .Setup(x => x.GenerateAccessToken(user))
                .Returns("access_token");

            _jwtServiceMock
                .Setup(x => x.GenerateRefreshToken())
                .Returns("refresh_token");

            await _handler.Handle(command, CancellationToken.None);

            _refreshTokenRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_SavesChanges()
        {
            var command = ValidCommand;
            var user = ValidUser;

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.Password, user.PasswordHash))
                .Returns(true);

            _jwtServiceMock
                .Setup(x => x.GenerateAccessToken(user))
                .Returns("access_token");

            _jwtServiceMock
                .Setup(x => x.GenerateRefreshToken())
                .Returns("refresh_token");

            await _handler.Handle(command, CancellationToken.None);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task Handle_ValidCommand_GeneratesTokens()
        {
            var command = ValidCommand;
            var user = ValidUser;

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.Password, user.PasswordHash))
                .Returns(true);

            _jwtServiceMock
                .Setup(x => x.GenerateAccessToken(user))
                .Returns("access_token");

            _jwtServiceMock
                .Setup(x => x.GenerateRefreshToken())
                .Returns("refresh_token");

            await _handler.Handle(command, CancellationToken.None);

            _jwtServiceMock.Verify(x => x.GenerateAccessToken(user), Times.Once);
            _jwtServiceMock.Verify(x => x.GenerateRefreshToken(), Times.Once);
        }
    }
}
