using blog.Application.Posts.Commands.DeletePost;
using blog.Domain.Categories.Entities;
using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Entities;
using blog.Domain.Posts.Repository;
using blog.Domain.Posts.Types;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Enums;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Posts.Commands
{
    public class DeletePostCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IPostRepository> _postRepositoryMock = new();
        private readonly Mock<IFileStorageService> _fileStorageServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly DeletePostCommandHandler _handler;

        public DeletePostCommandHandlerTests()
        {
            _handler = new DeletePostCommandHandler(
                _userRepositoryMock.Object,
                _postRepositoryMock.Object,
                _fileStorageServiceMock.Object,
                _unitOfWorkMock.Object);
        }

        private static User CreateUser(string email, UserLevel level)
        {
            var user = new User(email, "Ali", "Rezaei", "hashed_password");
            if (level != UserLevel.Normal)
                user.Promote(level);

            return user;
        }

        private static Post CreatePost(User author, string? titleImageUrl = null)
            => new("My First Post", titleImageUrl, "Some content", ["dotnet"], author, new Category("Technology"));

        private static DeletePostCommand ValidCommand(Guid actorId, Guid postId) => new()
        {
            ActorId = actorId,
            PostId = postId
        };

        private void SetupActorAndPost(User actor, Post post)
        {
            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(actor.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            _postRepositoryMock
                .Setup(x => x.GetByIdAsync(post.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(post);
        }

        [Fact]
        public async Task Handle_OwnerActor_ReturnsSuccessResponse()
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Author);
            var post = CreatePost(author);
            var command = ValidCommand(author.Id.Value, post.Id.Value);

            SetupActorAndPost(author, post);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
        }

        [Theory]
        [InlineData(UserLevel.Admin)]
        [InlineData(UserLevel.Owner)]
        public async Task Handle_ElevatedNonOwnerActor_ReturnsSuccessResponse(UserLevel actorLevel)
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Author);
            var actor = CreateUser("moderator@test.com", actorLevel);
            var post = CreatePost(author);
            var command = ValidCommand(actor.Id.Value, post.Id.Value);

            SetupActorAndPost(actor, post);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Success.Should().BeTrue();
        }

        [Theory]
        [InlineData(UserLevel.Normal)]
        [InlineData(UserLevel.Author)]
        public async Task Handle_NonOwnerNormalOrAuthorActor_ThrowsForbiddenException(UserLevel actorLevel)
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Author);
            var actor = CreateUser("other@test.com", actorLevel);
            var post = CreatePost(author);
            var command = ValidCommand(actor.Id.Value, post.Id.Value);

            SetupActorAndPost(actor, post);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task Handle_ActorNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var command = ValidCommand(Guid.NewGuid(), Guid.NewGuid());

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(command.ActorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_DeletedActor_ThrowsInvalidStateException()
        {
            // Arrange
            var actor = CreateUser("author@test.com", UserLevel.Author);
            actor.SoftDelete();
            var command = ValidCommand(actor.Id.Value, Guid.NewGuid());

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(actor.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_BannedActor_ThrowsInvalidStateException()
        {
            // Arrange
            var actor = CreateUser("author@test.com", UserLevel.Author);
            actor.Ban();
            var command = ValidCommand(actor.Id.Value, Guid.NewGuid());

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(actor.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_PostNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var actor = CreateUser("author@test.com", UserLevel.Author);
            var command = ValidCommand(actor.Id.Value, Guid.NewGuid());

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(actor.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            _postRepositoryMock
                .Setup(x => x.GetByIdAsync(new PostId(command.PostId), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Post?)null);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_PostWithTitleImage_DeletesImageFromStorage()
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Author);
            var post = CreatePost(author, "https://cloudinary.com/posts/cover.jpg");
            var command = ValidCommand(author.Id.Value, post.Id.Value);

            SetupActorAndPost(author, post);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _fileStorageServiceMock.Verify(
                x => x.DeleteAsync("https://cloudinary.com/posts/cover.jpg", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_PostWithoutTitleImage_DoesNotCallFileStorageService()
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Author);
            var post = CreatePost(author, titleImageUrl: null);
            var command = ValidCommand(author.Id.Value, post.Id.Value);

            SetupActorAndPost(author, post);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _fileStorageServiceMock.Verify(
                x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ValidCommand_DeletesPostFromRepository()
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Author);
            var post = CreatePost(author);
            var command = ValidCommand(author.Id.Value, post.Id.Value);

            SetupActorAndPost(author, post);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _postRepositoryMock.Verify(
                x => x.Delete(post),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_SavesChanges()
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Author);
            var post = CreatePost(author);
            var command = ValidCommand(author.Id.Value, post.Id.Value);

            SetupActorAndPost(author, post);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
