using blog.Application.Users.Commands.RefreshToken;
using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Tokens.Entities;
using blog.Domain.Tokens.Repository;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Users.Commands
{
    public class RefreshTokenCommandHandlerTests
    {
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IHasher> _tokenHasherMock = new();
        private readonly Mock<IJwtService> _jwtServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly RefreshTokenCommandHandler _handler;

        private const string PlainToken = "valid_refresh_token";
        private const string HashedToken = "hashed_valid_refresh_token";

        public RefreshTokenCommandHandlerTests()
        {
            _handler = new RefreshTokenCommandHandler(
                _refreshTokenRepositoryMock.Object,
                _userRepositoryMock.Object,
                _tokenHasherMock.Object,
                _jwtServiceMock.Object,
                _unitOfWorkMock.Object);

            _tokenHasherMock
                .Setup(x => x.Hash(PlainToken))
                .Returns(HashedToken);
        }

        private static RefreshTokenCommand ValidCommand => new()
        {
            RefreshToken = PlainToken,
            DeviceInfo = "Mozilla/5.0 Chrome/120"
        };

        private static User ValidUser => new(
            "test@test.com",
            "Ali",
            "Rezaei",
            "hashed_password");

        private static RefreshToken ValidToken(UserId userId)
            => new(HashedToken, userId, "Mozilla/5.0 Chrome/120", expiryDays: 30);

        [Fact]
        public async Task Handle_ValidCommand_ReturnsRefreshTokenResponse()
        {
            // Arrange
            var command = ValidCommand;
            var user = ValidUser;
            var token = ValidToken(user.Id);

            _refreshTokenRepositoryMock
                .Setup(x => x.GetByTokenHashAsync(HashedToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(token);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(token.UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _tokenHasherMock
                .Setup(x => x.Hash("new_refresh_token"))
                .Returns("hashed_new_refresh_token");

            _jwtServiceMock
                .Setup(x => x.GenerateRefreshToken())
                .Returns("new_refresh_token");

            _jwtServiceMock
                .Setup(x => x.GenerateAccessToken(user))
                .Returns("new_access_token");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.AccessToken.Should().Be("new_access_token");
            result.RefreshToken.Should().Be("new_refresh_token");
        }

        [Fact]
        public async Task Handle_TokenNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var command = ValidCommand;

            _refreshTokenRepositoryMock
                .Setup(x => x.GetByTokenHashAsync(HashedToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync((RefreshToken?)null);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_ExpiredToken_ThrowsExpiredException()
        {
            // Arrange
            var command = ValidCommand;
            var user = ValidUser;
            var token = new RefreshToken(HashedToken, user.Id, "Mozilla/5.0 Chrome/120", expiryDays: -1);

            _refreshTokenRepositoryMock
                .Setup(x => x.GetByTokenHashAsync(HashedToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(token);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ExpiredException>();
        }

        [Fact]
        public async Task Handle_UserNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var command = ValidCommand;
            var user = ValidUser;
            var token = ValidToken(user.Id);

            _refreshTokenRepositoryMock
                .Setup(x => x.GetByTokenHashAsync(HashedToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(token);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(token.UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_DeletedUser_ThrowsInvalidStateException()
        {
            // Arrange
            var command = ValidCommand;
            var user = ValidUser;
            var token = ValidToken(user.Id);
            user.SoftDelete();

            _refreshTokenRepositoryMock
                .Setup(x => x.GetByTokenHashAsync(HashedToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(token);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(token.UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_BannedUser_ThrowsInvalidStateException()
        {
            // Arrange
            var command = ValidCommand;
            var user = ValidUser;
            var token = ValidToken(user.Id);
            user.Ban();

            _refreshTokenRepositoryMock
                .Setup(x => x.GetByTokenHashAsync(HashedToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(token);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(token.UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_ValidCommand_RotatesToken()
        {
            // Arrange
            var command = ValidCommand;
            var user = ValidUser;
            var token = ValidToken(user.Id);

            _refreshTokenRepositoryMock
                .Setup(x => x.GetByTokenHashAsync(HashedToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(token);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(token.UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _tokenHasherMock
                .Setup(x => x.Hash("new_refresh_token"))
                .Returns("hashed_new_refresh_token");

            _jwtServiceMock
                .Setup(x => x.GenerateRefreshToken())
                .Returns("new_refresh_token");

            _jwtServiceMock
                .Setup(x => x.GenerateAccessToken(user))
                .Returns("new_access_token");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _refreshTokenRepositoryMock.Verify(
                x => x.Update(token),
                Times.Once);

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
            var token = ValidToken(user.Id);

            _refreshTokenRepositoryMock
                .Setup(x => x.GetByTokenHashAsync(HashedToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(token);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(token.UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _tokenHasherMock
                .Setup(x => x.Hash("new_refresh_token"))
                .Returns("hashed_new_refresh_token");

            _jwtServiceMock
                .Setup(x => x.GenerateRefreshToken())
                .Returns("new_refresh_token");

            _jwtServiceMock
                .Setup(x => x.GenerateAccessToken(user))
                .Returns("new_access_token");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_GeneratesNewTokens()
        {
            // Arrange
            var command = ValidCommand;
            var user = ValidUser;
            var token = ValidToken(user.Id);

            _refreshTokenRepositoryMock
                .Setup(x => x.GetByTokenHashAsync(HashedToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(token);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(token.UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _tokenHasherMock
                .Setup(x => x.Hash("new_refresh_token"))
                .Returns("hashed_new_refresh_token");

            _jwtServiceMock
                .Setup(x => x.GenerateRefreshToken())
                .Returns("new_refresh_token");

            _jwtServiceMock
                .Setup(x => x.GenerateAccessToken(user))
                .Returns("new_access_token");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _jwtServiceMock.Verify(x => x.GenerateAccessToken(user), Times.Once);
            _jwtServiceMock.Verify(x => x.GenerateRefreshToken(), Times.Once);
        }
    }
}
