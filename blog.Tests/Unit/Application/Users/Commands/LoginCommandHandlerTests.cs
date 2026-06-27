using blog.Application.Users.Commands.Login;
using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Tokens.Entities;
using blog.Domain.Tokens.Repository;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Repository;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Users.Commands
{
    public class LoginCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
        private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
        private readonly Mock<IJwtService> _jwtServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly LoginCommandHandler _handler;

        public LoginCommandHandlerTests()
        {
            _handler = new LoginCommandHandler(
                _userRepositoryMock.Object,
                _refreshTokenRepositoryMock.Object,
                _passwordHasherMock.Object,
                _jwtServiceMock.Object,
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
            // Arrange
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

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.AccessToken.Should().Be("access_token");
            result.RefreshToken.Should().Be("refresh_token");
        }

        [Fact]
        public async Task Handle_UserNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var command = ValidCommand;

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_InvalidPassword_ThrowsValidationException()
        {
            // Arrange
            var command = ValidCommand;
            var user = ValidUser;

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
        public async Task Handle_ValidCommand_AddsRefreshToken()
        {
            // Arrange
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

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _refreshTokenRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_SavesChanges()
        {
            // Arrange
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

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_GeneratesTokens()
        {
            // Arrange
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

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _jwtServiceMock.Verify(x => x.GenerateAccessToken(user), Times.Once);
            _jwtServiceMock.Verify(x => x.GenerateRefreshToken(), Times.Once);
        }
    }
}
