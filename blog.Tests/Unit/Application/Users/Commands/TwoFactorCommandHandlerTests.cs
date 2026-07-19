using blog.Application.Users.Commands.TwoFactor;
using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Users.Commands
{
    public class TwoFactorCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly TwoFactorCommandHandler _handler;

        public TwoFactorCommandHandlerTests()
        {
            _handler = new TwoFactorCommandHandler(
                _userRepositoryMock.Object,
                _unitOfWorkMock.Object);
        }

        private static User CreateConfirmedUser()
        {
            var user = new User("test@test.com", "Ali", "Rezaei", "hashed_password");
            user.ConfirmEmail();
            return user;
        }

        private static TwoFactorCommand CreateCommand(Guid userId, bool twoFactor) => new()
        {
            UserId = userId,
            TwoFactor = twoFactor
        };

        // ── User not found ──────────────────────────────────────────────

        [Fact]
        public async Task Handle_UserNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var command = CreateCommand(Guid.NewGuid(), twoFactor: true);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(command.UserId), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        // ── Inactive user ────────────────────────────────────────────────

        [Fact]
        public async Task Handle_UserIsBanned_ThrowsInvalidStateException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            user.Ban();

            var command = CreateCommand(user.Id.Value, twoFactor: true);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_UserIsDeleted_ThrowsInvalidStateException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            user.SoftDelete();

            var command = CreateCommand(user.Id.Value, twoFactor: true);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        // ── Email not confirmed ─────────────────────────────────────────

        [Fact]
        public async Task Handle_EmailNotConfirmed_ThrowsEmailNotConfirmedException()
        {
            // Arrange
            var user = new User("test@test.com", "Ali", "Rezaei", "hashed_password");
            var command = CreateCommand(user.Id.Value, twoFactor: true);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<EmailNotConfirmedException>();
        }

        // ── Already in requested state ───────────────────────────────────

        [Fact]
        public async Task Handle_TwoFactorAlreadyEnabled_ThrowsInvalidStateException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            user.EnableTwoFactor();

            var command = CreateCommand(user.Id.Value, twoFactor: true);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_TwoFactorAlreadyDisabled_ThrowsInvalidStateException()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value, twoFactor: false);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_AlreadyInRequestedState_DoesNotSaveChanges()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value, twoFactor: false);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<InvalidStateException>();

            // Assert
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ── Successful enable ────────────────────────────────────────────

        [Fact]
        public async Task Handle_EnableTwoFactor_ReturnsEnabledResponse()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value, twoFactor: true);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Id.Should().Be(user.Id.Value);
            result.TwoFactorEnabled.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_EnableTwoFactor_SetsUserTwoFactorEnabledTrue()
        {
            // Arrange
            var user = CreateConfirmedUser();
            var command = CreateCommand(user.Id.Value, twoFactor: true);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            user.TwoFactorEnabled.Should().BeTrue();
            _userRepositoryMock.Verify(x => x.Update(user), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ── Successful disable ───────────────────────────────────────────

        [Fact]
        public async Task Handle_DisableTwoFactor_ReturnsDisabledResponse()
        {
            // Arrange
            var user = CreateConfirmedUser();
            user.EnableTwoFactor();

            var command = CreateCommand(user.Id.Value, twoFactor: false);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Id.Should().Be(user.Id.Value);
            result.TwoFactorEnabled.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_DisableTwoFactor_SetsUserTwoFactorEnabledFalse()
        {
            // Arrange
            var user = CreateConfirmedUser();
            user.EnableTwoFactor();

            var command = CreateCommand(user.Id.Value, twoFactor: false);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            user.TwoFactorEnabled.Should().BeFalse();
            _userRepositoryMock.Verify(x => x.Update(user), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}