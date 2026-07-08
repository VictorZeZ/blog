using blog.Application.Posts.Commands.ChangePostStatus;
using blog.Domain.Categories.Types;
using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Entities;
using blog.Domain.Posts.Enums;
using blog.Domain.Posts.Repository;
using blog.Domain.Posts.Types;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Enums;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Posts.Commands
{
    public class ChangePostStatusCommandHandlerTests
    {
        private readonly Mock<IPostRepository> _postRepositoryMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly ChangePostStatusCommandHandler _handler;

        public ChangePostStatusCommandHandlerTests()
        {
            _handler = new ChangePostStatusCommandHandler(
                _postRepositoryMock.Object,
                _unitOfWorkMock.Object);
        }

        private static User CreateActor(UserLevel level)
        {
            var user = new User("actor@test.com", "Ali", "Rezaei", "hashed_password");
            if (level != UserLevel.Normal)
                user.Promote(level);

            return user;
        }

        private static Post CreatePost(UserLevel authorLevel)
        {
            var author = CreateActor(authorLevel);
            return new Post("My First Post", null, "Some content", ["dotnet"], author, CategoryId.New());
        }

        [Fact]
        public async Task Handle_ApproveAction_ReturnsPublishedStatus()
        {
            // Arrange
            var post = CreatePost(UserLevel.Author);

            var command = new ChangePostStatusCommand
            {
                ActorId = Guid.NewGuid(),
                PostId = post.Id.Value,
                Action = ChangePostStatusAction.Approve
            };

            _postRepositoryMock
                .Setup(x => x.GetByIdAsync(new PostId(command.PostId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(post);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(PostStatus.Published);
            result.Id.Should().Be(post.Id.Value);
        }

        [Fact]
        public async Task Handle_RejectAction_ReturnsRejectedStatus()
        {
            // Arrange
            var post = CreatePost(UserLevel.Author);

            var command = new ChangePostStatusCommand
            {
                ActorId = Guid.NewGuid(),
                PostId = post.Id.Value,
                Action = ChangePostStatusAction.Reject
            };

            _postRepositoryMock
                .Setup(x => x.GetByIdAsync(new PostId(command.PostId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(post);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(PostStatus.Rejected);
            result.Id.Should().Be(post.Id.Value);
        }

        [Fact]
        public async Task Handle_PostNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var command = new ChangePostStatusCommand
            {
                ActorId = Guid.NewGuid(),
                PostId = Guid.NewGuid(),
                Action = ChangePostStatusAction.Approve
            };

            _postRepositoryMock
                .Setup(x => x.GetByIdAsync(new PostId(command.PostId), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Post?)null);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_ApproveAlreadyPublishedPost_ThrowsInvalidStateException()
        {
            // Arrange
            var post = CreatePost(UserLevel.Admin); // Admin-authored post is auto-published

            var command = new ChangePostStatusCommand
            {
                ActorId = Guid.NewGuid(),
                PostId = post.Id.Value,
                Action = ChangePostStatusAction.Approve
            };

            _postRepositoryMock
                .Setup(x => x.GetByIdAsync(new PostId(command.PostId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(post);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_RejectAlreadyRejectedPost_ThrowsInvalidStateException()
        {
            // Arrange
            var post = CreatePost(UserLevel.Author);
            post.Reject();

            var command = new ChangePostStatusCommand
            {
                ActorId = Guid.NewGuid(),
                PostId = post.Id.Value,
                Action = ChangePostStatusAction.Reject
            };

            _postRepositoryMock
                .Setup(x => x.GetByIdAsync(new PostId(command.PostId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(post);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_ValidCommand_UpdatesPostInRepository()
        {
            // Arrange
            var post = CreatePost(UserLevel.Author);

            var command = new ChangePostStatusCommand
            {
                ActorId = Guid.NewGuid(),
                PostId = post.Id.Value,
                Action = ChangePostStatusAction.Approve
            };

            _postRepositoryMock
                .Setup(x => x.GetByIdAsync(new PostId(command.PostId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(post);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _postRepositoryMock.Verify(
                x => x.Update(It.IsAny<Post>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_SavesChanges()
        {
            // Arrange
            var post = CreatePost(UserLevel.Author);

            var command = new ChangePostStatusCommand
            {
                ActorId = Guid.NewGuid(),
                PostId = post.Id.Value,
                Action = ChangePostStatusAction.Approve
            };

            _postRepositoryMock
                .Setup(x => x.GetByIdAsync(new PostId(command.PostId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(post);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
