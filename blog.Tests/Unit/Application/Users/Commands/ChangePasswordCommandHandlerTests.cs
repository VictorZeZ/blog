using blog.Application.Users.Commands.ChangePassword;
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
    public class ChangePasswordCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
        private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly ChangePasswordCommandHandler _handler;

        public ChangePasswordCommandHandlerTests()
        {
            _handler = new ChangePasswordCommandHandler(
                _userRepositoryMock.Object,
                _refreshTokenRepositoryMock.Object,
                _passwordHasherMock.Object,
                _unitOfWorkMock.Object);
        }

        private static ChangePasswordCommand ValidCommand => new()
        {
            UserId = Guid.NewGuid(),
            CurrentPassword = "OldPassword123",
            NewPassword = "NewPassword123"
        };

        private static User ValidUser => new(
            "test@test.com",
            "Ali",
            "Rezaei",
            "hashed_old_password");

        private void SetupNoActiveTokens(Guid userId)
        {
            _refreshTokenRepositoryMock
                .Setup(x => x.GetActiveByUserIdAsync(new UserId(userId), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
        }

        [Fact]
        public async Task Handle_ValidCommand_ReturnsSuccessResponse()
        {
            // Arrange
            var command = ValidCommand;
            var user = ValidUser;

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(command.UserId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.CurrentPassword, user.PasswordHash))
                .Returns(true);

            _passwordHasherMock
                .Setup(x => x.Hash(command.NewPassword))
                .Returns("hashed_new_password");

            SetupNoActiveTokens(command.UserId);

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
        public async Task Handle_DeletedUser_ThrowsInvalidStateException()
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
        public async Task Handle_InvalidCurrentPassword_ThrowsValidationException()
        {
            // Arrange
            var command = ValidCommand;
            var user = ValidUser;

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(command.UserId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.CurrentPassword, user.PasswordHash))
                .Returns(false);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task Handle_ValidCommand_HashesNewPassword()
        {
            // Arrange
            var command = ValidCommand;
            var user = ValidUser;

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(command.UserId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.CurrentPassword, user.PasswordHash))
                .Returns(true);

            _passwordHasherMock
                .Setup(x => x.Hash(command.NewPassword))
                .Returns("hashed_new_password");

            SetupNoActiveTokens(command.UserId);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _passwordHasherMock.Verify(x => x.Hash(command.NewPassword), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_UpdatesUser()
        {
            // Arrange
            var command = ValidCommand;
            var user = ValidUser;

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(command.UserId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.CurrentPassword, user.PasswordHash))
                .Returns(true);

            _passwordHasherMock
                .Setup(x => x.Hash(command.NewPassword))
                .Returns("hashed_new_password");

            SetupNoActiveTokens(command.UserId);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _userRepositoryMock.Verify(
                x => x.Update(It.IsAny<User>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_RevokesAllActiveRefreshTokens()
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

            _passwordHasherMock
                .Setup(x => x.Verify(command.CurrentPassword, user.PasswordHash))
                .Returns(true);

            _passwordHasherMock
                .Setup(x => x.Hash(command.NewPassword))
                .Returns("hashed_new_password");

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
        public async Task Handle_InvalidCurrentPassword_DoesNotRevokeTokensOrSaveChanges()
        {
            // Arrange
            var command = ValidCommand;
            var user = ValidUser;

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(command.UserId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _passwordHasherMock
                .Setup(x => x.Verify(command.CurrentPassword, user.PasswordHash))
                .Returns(false);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<ValidationException>();

            // Assert
            _refreshTokenRepositoryMock.Verify(
                x => x.GetActiveByUserIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
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

            _passwordHasherMock
                .Setup(x => x.Verify(command.CurrentPassword, user.PasswordHash))
                .Returns(true);

            _passwordHasherMock
                .Setup(x => x.Hash(command.NewPassword))
                .Returns("hashed_new_password");

            SetupNoActiveTokens(command.UserId);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
