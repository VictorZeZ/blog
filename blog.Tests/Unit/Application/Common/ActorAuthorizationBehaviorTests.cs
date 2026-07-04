using blog.Application.Common;
using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Enums;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using FluentAssertions;
using MediatR;
using Moq;

namespace blog.Tests.Unit.Application.Common
{
    public class ActorAuthorizationBehaviorTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly ActorAuthorizationBehavior<FakeRequest, string> _behavior;

        public ActorAuthorizationBehaviorTests()
        {
            _behavior = new ActorAuthorizationBehavior<FakeRequest, string>(_userRepositoryMock.Object);
        }

        public class FakeRequest : IRequest<string>, IRequireActorLevel
        {
            public Guid ActorId { get; init; }
            public UserLevel MinimumLevel { get; init; } = UserLevel.Admin;
        }

        private static User CreateUser(string email, UserLevel level)
        {
            var user = new User(email, "Ali", "Rezaei", "hashed_password");
            if (level != UserLevel.Normal)
                user.Promote(level);
            return user;
        }

        private static RequestHandlerDelegate<string> NextDelegate(string result = "ok")
            => (_) => Task.FromResult(result);

        [Fact]
        public async Task Handle_ActorMeetsMinimumLevel_CallsNext()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var actor = CreateUser("admin@test.com", UserLevel.Admin);
            var request = new FakeRequest { ActorId = actorId, MinimumLevel = UserLevel.Admin };

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(actorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            // Act
            var result = await _behavior.Handle(request, NextDelegate(), CancellationToken.None);

            // Assert
            result.Should().Be("ok");
        }

        [Theory]
        [InlineData(UserLevel.Normal)]
        [InlineData(UserLevel.Author)]
        public async Task Handle_ActorBelowMinimumLevel_ThrowsForbiddenException(UserLevel level)
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var actor = CreateUser("user@test.com", level);
            var request = new FakeRequest { ActorId = actorId, MinimumLevel = UserLevel.Admin };

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(actorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            // Act
            var act = () => _behavior.Handle(request, NextDelegate(), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task Handle_ActorNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var request = new FakeRequest { ActorId = actorId, MinimumLevel = UserLevel.Admin };

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(actorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var act = () => _behavior.Handle(request, NextDelegate(), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_DeletedActor_ThrowsInvalidStateException()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var actor = CreateUser("admin@test.com", UserLevel.Admin);
            actor.SoftDelete();
            var request = new FakeRequest { ActorId = actorId, MinimumLevel = UserLevel.Admin };

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(actorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            // Act
            var act = () => _behavior.Handle(request, NextDelegate(), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_BannedActor_ThrowsInvalidStateException()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var actor = CreateUser("admin@test.com", UserLevel.Admin);
            actor.Ban();
            var request = new FakeRequest { ActorId = actorId, MinimumLevel = UserLevel.Admin };

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(actorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            // Act
            var act = () => _behavior.Handle(request, NextDelegate(), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_RequestNotIRequireActorLevel_SkipsCheckAndCallsNext()
        {
            // Arrange — یک request عادی که IRequireActorLevel را پیاده نمی‌کند
            var behavior = new ActorAuthorizationBehavior<PlainRequest, string>(_userRepositoryMock.Object);
            var request = new PlainRequest();

            // Act
            var result = await behavior.Handle(request, NextDelegate(), CancellationToken.None);

            // Assert
            result.Should().Be("ok");
            _userRepositoryMock.Verify(
                x => x.GetByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        public class PlainRequest : IRequest<string> { }
    }
}
