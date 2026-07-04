using blog.Application.Posts.Commands.UpdatePost;
using blog.Domain.Common.Enum;
using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Entities;
using blog.Domain.Posts.Repository;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Enums;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Posts.Commands
{
    public class UpdatePostCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IPostRepository> _postRepositoryMock = new();
        private readonly Mock<IFileStorageService> _fileStorageServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly UpdatePostCommandHandler _handler;

        public UpdatePostCommandHandlerTests()
        {
            _handler = new UpdatePostCommandHandler(
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
            => new("Original Title", titleImageUrl, "Original content", ["dotnet"], author);

        private static UpdatePostCommand ValidCommand(Guid actorId, Guid postId) => new()
        {
            ActorId = actorId,
            PostId = postId,
            Title = "Updated Title",
            Content = "Updated content",
            Tags = ["dotnet", "csharp"]
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
        public async Task Handle_OwnerActor_ReturnsUpdatePostResponse()
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
            result.Id.Should().Be(post.Id.Value);
            result.Title.Should().Be(command.Title);
            result.Slug.Should().Be("updated-title");
        }

        [Theory]
        [InlineData(UserLevel.Admin)]
        [InlineData(UserLevel.Owner)]
        public async Task Handle_ElevatedNonOwnerActor_ReturnsUpdatePostResponse(UserLevel actorLevel)
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
            result.Should().NotBeNull();
            result.Title.Should().Be(command.Title);
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
                .Setup(x => x.GetByIdAsync(new blog.Domain.Posts.Types.PostId(command.PostId), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Post?)null);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_TitleChangedToExistingSlug_ThrowsAlreadyExistsException()
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Author);
            var post = CreatePost(author);
            var command = ValidCommand(author.Id.Value, post.Id.Value);

            SetupActorAndPost(author, post);

            _postRepositoryMock
                .Setup(x => x.ExistsBySlugAsync("updated-title", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<AlreadyExistsException>();
        }

        [Fact]
        public async Task Handle_TitleUnchanged_DoesNotCheckSlugExistence()
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Author);
            var post = CreatePost(author);

            var command = new UpdatePostCommand
            {
                ActorId = author.Id.Value,
                PostId = post.Id.Value,
                Title = post.Title,
                Content = "Updated content",
                Tags = ["dotnet", "csharp"]
            };

            SetupActorAndPost(author, post);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _postRepositoryMock.Verify(
                x => x.ExistsBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_TitleChangedToUniqueSlug_UpdatesSlug()
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Author);
            var post = CreatePost(author);
            var command = ValidCommand(author.Id.Value, post.Id.Value);

            SetupActorAndPost(author, post);

            _postRepositoryMock
                .Setup(x => x.ExistsBySlugAsync("updated-title", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Slug.Should().Be("updated-title");
        }

        [Fact]
        public async Task Handle_NoImageChangeRequested_KeepsCurrentImageUrl()
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Author);
            var post = CreatePost(author, "https://cloudinary.com/posts/old.jpg");
            var command = ValidCommand(author.Id.Value, post.Id.Value);

            SetupActorAndPost(author, post);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.TitleImageUrl.Should().Be("https://cloudinary.com/posts/old.jpg");
            _fileStorageServiceMock.Verify(
                x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<StorageFolder>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _fileStorageServiceMock.Verify(
                x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WithNewTitleImageStream_UploadsNewAndDeletesOldImage()
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Author);
            var post = CreatePost(author, "https://cloudinary.com/posts/old.jpg");
            using var stream = new MemoryStream();

            var command = new UpdatePostCommand
            {
                ActorId = author.Id.Value,
                PostId = post.Id.Value,
                Title = "Updated Title",
                Content = "Updated content",
                Tags = ["dotnet", "csharp"],
                TitleImageStream = stream,
                TitleImageFileName = "new-cover.jpg",
                TitleImageContentType = "image/jpeg",
                TitleImageSizeBytes = 1024 * 500
            };

            SetupActorAndPost(author, post);

            _fileStorageServiceMock
                .Setup(x => x.UploadAsync(stream, "new-cover.jpg", StorageFolder.Posts, It.IsAny<CancellationToken>()))
                .ReturnsAsync("https://cloudinary.com/posts/new-cover.jpg");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.TitleImageUrl.Should().Be("https://cloudinary.com/posts/new-cover.jpg");

            _fileStorageServiceMock.Verify(
                x => x.UploadAsync(stream, "new-cover.jpg", StorageFolder.Posts, It.IsAny<CancellationToken>()),
                Times.Once);

            _fileStorageServiceMock.Verify(
                x => x.DeleteAsync("https://cloudinary.com/posts/old.jpg", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithNewTitleImageStreamAndNoExistingImage_DoesNotCallDelete()
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Author);
            var post = CreatePost(author, titleImageUrl: null);
            using var stream = new MemoryStream();

            var command = new UpdatePostCommand
            {
                ActorId = author.Id.Value,
                PostId = post.Id.Value,
                Title = "Updated Title",
                Content = "Updated content",
                Tags = ["dotnet", "csharp"],
                TitleImageStream = stream,
                TitleImageFileName = "new-cover.jpg",
                TitleImageContentType = "image/jpeg",
                TitleImageSizeBytes = 1024 * 500
            };

            SetupActorAndPost(author, post);

            _fileStorageServiceMock
                .Setup(x => x.UploadAsync(stream, "new-cover.jpg", StorageFolder.Posts, It.IsAny<CancellationToken>()))
                .ReturnsAsync("https://cloudinary.com/posts/new-cover.jpg");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _fileStorageServiceMock.Verify(
                x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_TitleImageTooLarge_ThrowsPayloadTooLargeException()
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Author);
            var post = CreatePost(author);
            using var stream = new MemoryStream();

            var command = new UpdatePostCommand
            {
                ActorId = author.Id.Value,
                PostId = post.Id.Value,
                Title = "Updated Title",
                Content = "Updated content",
                Tags = ["dotnet"],
                TitleImageStream = stream,
                TitleImageFileName = "cover.jpg",
                TitleImageContentType = "image/jpeg",
                TitleImageSizeBytes = 10 * 1024 * 1024
            };

            SetupActorAndPost(author, post);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<PayloadTooLargeException>();
        }

        [Fact]
        public async Task Handle_UnsupportedContentType_ThrowsUnsupportedMediaTypeException()
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Author);
            var post = CreatePost(author);
            using var stream = new MemoryStream();

            var command = new UpdatePostCommand
            {
                ActorId = author.Id.Value,
                PostId = post.Id.Value,
                Title = "Updated Title",
                Content = "Updated content",
                Tags = ["dotnet"],
                TitleImageStream = stream,
                TitleImageFileName = "cover.pdf",
                TitleImageContentType = "application/pdf",
                TitleImageSizeBytes = 1024
            };

            SetupActorAndPost(author, post);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<UnsupportedMediaTypeException>();
        }

        [Fact]
        public async Task Handle_RemoveTitleImage_DeletesOldImageAndSetsNull()
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Author);
            var post = CreatePost(author, "https://cloudinary.com/posts/old.jpg");

            var command = new UpdatePostCommand
            {
                ActorId = author.Id.Value,
                PostId = post.Id.Value,
                Title = "Updated Title",
                Content = "Updated content",
                Tags = ["dotnet", "csharp"],
                RemoveTitleImage = true
            };

            SetupActorAndPost(author, post);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.TitleImageUrl.Should().BeNull();

            _fileStorageServiceMock.Verify(
                x => x.DeleteAsync("https://cloudinary.com/posts/old.jpg", It.IsAny<CancellationToken>()),
                Times.Once);

            _fileStorageServiceMock.Verify(
                x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<StorageFolder>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_RemoveTitleImageWithNoExistingImage_DoesNotCallDelete()
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Author);
            var post = CreatePost(author, titleImageUrl: null);

            var command = new UpdatePostCommand
            {
                ActorId = author.Id.Value,
                PostId = post.Id.Value,
                Title = "Updated Title",
                Content = "Updated content",
                Tags = ["dotnet", "csharp"],
                RemoveTitleImage = true
            };

            SetupActorAndPost(author, post);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.TitleImageUrl.Should().BeNull();

            _fileStorageServiceMock.Verify(
                x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ValidCommand_UpdatesPostInRepository()
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
                x => x.Update(It.IsAny<Post>()),
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
