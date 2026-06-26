using blog.Application.Users.Commands.BanUser;
using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Enums;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Users
{
    public class BanUserCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly BanUserCommandHandler _handler;

        public BanUserCommandHandlerTests()
        {
            _handler = new BanUserCommandHandler(
                _userRepositoryMock.Object,
                _unitOfWorkMock.Object);
        }

        private static User CreateUser(string email, UserLevel level)
        {
            var user = new User(email, "Ali", "Rezaei", "hashed_password");
            if (level != UserLevel.Normal)
                user.Promote(level);
            return user;
        }

        private static BanUserCommand BanCommand(Guid actorId, Guid targetId) => new()
        {
            ActorId = actorId,
            TargetUserId = targetId,
            IsBanned = true
        };

        private static BanUserCommand UnbanCommand(Guid actorId, Guid targetId) => new()
        {
            ActorId = actorId,
            TargetUserId = targetId,
            IsBanned = false
        };

        [Fact]
        public async Task Handle_AdminBanningNormalUser_ReturnsSuccess()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            var actor = CreateUser("admin@test.com", UserLevel.Admin);
            var target = CreateUser("normal@test.com", UserLevel.Normal);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(actorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(targetId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(target);

            // Act
            var result = await _handler.Handle(BanCommand(actorId, targetId), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsBanned.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_AdminUnbanningNormalUser_ReturnsSuccess()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            var actor = CreateUser("admin@test.com", UserLevel.Admin);
            var target = CreateUser("normal@test.com", UserLevel.Normal);
            target.Ban();

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(actorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(targetId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(target);

            // Act
            var result = await _handler.Handle(UnbanCommand(actorId, targetId), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsBanned.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_NormalUserBanning_ThrowsForbiddenException()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            var actor = CreateUser("normal@test.com", UserLevel.Normal);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(actorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            // Act
            var act = () => _handler.Handle(BanCommand(actorId, targetId), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task Handle_AuthorBanning_ThrowsForbiddenException()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            var actor = CreateUser("author@test.com", UserLevel.Author);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(actorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            // Act
            var act = () => _handler.Handle(BanCommand(actorId, targetId), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task Handle_BanningOwner_ThrowsForbiddenException()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            var actor = CreateUser("admin@test.com", UserLevel.Admin);
            var target = CreateUser("owner@test.com", UserLevel.Owner);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(actorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(targetId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(target);

            // Act
            var act = () => _handler.Handle(BanCommand(actorId, targetId), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task Handle_AdminBanningAdmin_ThrowsForbiddenException()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            var actor = CreateUser("admin@test.com", UserLevel.Admin);
            var target = CreateUser("admin2@test.com", UserLevel.Admin);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(actorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(targetId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(target);

            // Act
            var act = () => _handler.Handle(BanCommand(actorId, targetId), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task Handle_AlreadyBanned_ThrowsInvalidStateException()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            var actor = CreateUser("admin@test.com", UserLevel.Admin);
            var target = CreateUser("normal@test.com", UserLevel.Normal);
            target.Ban();

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(actorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(targetId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(target);

            // Act
            var act = () => _handler.Handle(BanCommand(actorId, targetId), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_AlreadyActive_ThrowsInvalidStateException()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            var actor = CreateUser("admin@test.com", UserLevel.Admin);
            var target = CreateUser("normal@test.com", UserLevel.Normal);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(actorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(targetId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(target);

            // Act
            var act = () => _handler.Handle(UnbanCommand(actorId, targetId), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_ActorNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var targetId = Guid.NewGuid();

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(actorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var act = () => _handler.Handle(BanCommand(actorId, targetId), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_TargetNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            var actor = CreateUser("admin@test.com", UserLevel.Admin);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(actorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(targetId), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var act = () => _handler.Handle(BanCommand(actorId, targetId), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_ValidCommand_SavesChanges()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            var actor = CreateUser("admin@test.com", UserLevel.Admin);
            var target = CreateUser("normal@test.com", UserLevel.Normal);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(actorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(targetId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(target);

            // Act
            await _handler.Handle(BanCommand(actorId, targetId), CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
