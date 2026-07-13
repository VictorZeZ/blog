using blog.Application.Users.Commands.Logout;
using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Tokens.Entities;
using blog.Domain.Tokens.Enums;
using blog.Domain.Tokens.Repository;
using blog.Domain.Users.Types;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Users.Commands
{
    public class LogoutCommandHandlerTests
    {
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
        private readonly Mock<IHasher> _tokenHasherMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly LogoutCommandHandler _handler;

        private const string PlainToken = "valid_refresh_token";
        private const string HashedToken = "hashed_valid_refresh_token";

        public LogoutCommandHandlerTests()
        {
            _handler = new LogoutCommandHandler(
                _refreshTokenRepositoryMock.Object,
                _tokenHasherMock.Object,
                _unitOfWorkMock.Object);

            _tokenHasherMock
                .Setup(x => x.Hash(PlainToken))
                .Returns(HashedToken);
        }

        private static LogoutCommand ValidCommand => new()
        {
            RefreshToken = PlainToken
        };

        private static RefreshToken ValidToken => new(
            HashedToken,
            UserId.New(),
            "Mozilla/5.0 Chrome/120");

        [Fact]
        public async Task Handle_ValidCommand_ReturnsSuccessResponse()
        {
            // Arrange
            var command = ValidCommand;
            var token = ValidToken;

            _refreshTokenRepositoryMock
                .Setup(x => x.GetByTokenHashAsync(HashedToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(token);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
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
            var token = new RefreshToken(
                HashedToken,
                UserId.New(),
                "Mozilla/5.0 Chrome/120",
                expiryDays: -1);

            _refreshTokenRepositoryMock
                .Setup(x => x.GetByTokenHashAsync(HashedToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(token);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ExpiredException>();
        }

        [Fact]
        public async Task Handle_ValidCommand_RevokesToken()
        {
            // Arrange
            var command = ValidCommand;
            var token = ValidToken;

            _refreshTokenRepositoryMock
                .Setup(x => x.GetByTokenHashAsync(HashedToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(token);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _refreshTokenRepositoryMock.Verify(
                x => x.Update(It.Is<RefreshToken>(t => t.Status == TokenStatus.Revoked)),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_SavesChanges()
        {
            // Arrange
            var command = ValidCommand;
            var token = ValidToken;

            _refreshTokenRepositoryMock
                .Setup(x => x.GetByTokenHashAsync(HashedToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(token);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
