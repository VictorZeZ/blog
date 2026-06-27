using blog.Application.Users.Commands.DeleteAccount;
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
    public class DeleteAccountCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly DeleteAccountCommandHandler _handler;

        public DeleteAccountCommandHandlerTests()
        {
            _handler = new DeleteAccountCommandHandler(
                _userRepositoryMock.Object,
                _refreshTokenRepositoryMock.Object,
                _unitOfWorkMock.Object);
        }

        private static DeleteAccountCommand ValidCommand => new()
        {
            UserId = Guid.NewGuid()
        };

        private static User ValidUser => new(
            "test@test.com",
            "Ali",
            "Rezaei",
            "hashed_password");

        [Fact]
        public async Task Handle_ValidCommand_ReturnsSuccessResponse()
        {
            // Arrange
            var command = ValidCommand;
            var user = ValidUser;

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(command.UserId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _refreshTokenRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(new UserId(command.UserId), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_UserNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var command = ValidCommand;

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(command.UserId), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_AlreadyDeletedUser_ThrowsInvalidStateException()
        {
            // Arrange
            var command = ValidCommand;
            var user = ValidUser;
            user.SoftDelete();

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(command.UserId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_ValidCommand_RevokesAllActiveTokens()
        {
            // Arrange
            var command = ValidCommand;
            var user = ValidUser;
            var userId = new UserId(command.UserId);

            var activeTokens = new List<RefreshToken>
        {
            new("token_1", userId, "Chrome"),
            new("token_2", userId, "Firefox")
        };

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _refreshTokenRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(activeTokens);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _refreshTokenRepositoryMock.Verify(
                x => x.Update(It.IsAny<RefreshToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task Handle_ValidCommand_SoftDeletesUser()
        {
            // Arrange
            var command = ValidCommand;
            var user = ValidUser;

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(command.UserId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _refreshTokenRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(new UserId(command.UserId), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _userRepositoryMock.Verify(
                x => x.SoftDelete(It.IsAny<User>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_SavesChanges()
        {
            // Arrange
            var command = ValidCommand;
            var user = ValidUser;

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(command.UserId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _refreshTokenRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(new UserId(command.UserId), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
