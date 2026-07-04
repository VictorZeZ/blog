using blog.Application.Users.Commands.ChangeUserLevel;
using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Enums;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Users.Commands
{
    public class ChangeUserLevelCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly ChangeUserLevelCommandHandler _handler;

        public ChangeUserLevelCommandHandlerTests()
        {
            _handler = new ChangeUserLevelCommandHandler(
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

        private static ChangeUserLevelCommand ValidCommand(Guid actorId, Guid targetId) => new()
        {
            ActorId = actorId,
            TargetUserId = targetId,
            Level = UserLevel.Author
        };

        private void SetupActorAndTarget(User actor, Guid actorId, User? target, Guid targetId)
        {
            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(actorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(targetId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(target);
        }

        [Fact]
        public async Task Handle_OwnerChangingNormalUser_ReturnsSuccess()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            var actor = CreateUser("owner@test.com", UserLevel.Owner);
            var target = CreateUser("normal@test.com", UserLevel.Normal);

            SetupActorAndTarget(actor, actorId, target, targetId);

            // Act
            var result = await _handler.Handle(ValidCommand(actorId, targetId), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Level.Should().Be(UserLevel.Author);
        }

        [Fact]
        public async Task Handle_AdminChangingNormalUser_ReturnsSuccess()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            var actor = CreateUser("admin@test.com", UserLevel.Admin);
            var target = CreateUser("normal@test.com", UserLevel.Normal);

            SetupActorAndTarget(actor, actorId, target, targetId);

            // Act
            var result = await _handler.Handle(ValidCommand(actorId, targetId), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Level.Should().Be(UserLevel.Author);
        }

        [Fact]
        public async Task Handle_ChangingOwner_ThrowsForbiddenException()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            var actor = CreateUser("admin@test.com", UserLevel.Admin);
            var target = CreateUser("owner@test.com", UserLevel.Owner);

            SetupActorAndTarget(actor, actorId, target, targetId);

            // Act
            var act = () => _handler.Handle(ValidCommand(actorId, targetId), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task Handle_AdminChangingAdmin_ThrowsForbiddenException()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            var actor = CreateUser("admin@test.com", UserLevel.Admin);
            var target = CreateUser("admin2@test.com", UserLevel.Admin);
            var command = new ChangeUserLevelCommand
            {
                ActorId = actorId,
                TargetUserId = targetId,
                Level = UserLevel.Normal
            };

            SetupActorAndTarget(actor, actorId, target, targetId);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task Handle_SameLevel_ThrowsInvalidStateException()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            var actor = CreateUser("owner@test.com", UserLevel.Owner);
            var target = CreateUser("author@test.com", UserLevel.Author);
            var command = new ChangeUserLevelCommand
            {
                ActorId = actorId,
                TargetUserId = targetId,
                Level = UserLevel.Author
            };

            SetupActorAndTarget(actor, actorId, target, targetId);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

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
            var act = () => _handler.Handle(ValidCommand(actorId, targetId), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_TargetNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            var actor = CreateUser("owner@test.com", UserLevel.Owner);

            SetupActorAndTarget(actor, actorId, null, targetId);

            // Act
            var act = () => _handler.Handle(ValidCommand(actorId, targetId), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_ValidCommand_SavesChanges()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            var actor = CreateUser("owner@test.com", UserLevel.Owner);
            var target = CreateUser("normal@test.com", UserLevel.Normal);

            SetupActorAndTarget(actor, actorId, target, targetId);

            // Act
            await _handler.Handle(ValidCommand(actorId, targetId), CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
