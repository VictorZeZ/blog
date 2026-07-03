using blog.Application.Posts.Commands.CreatePost;
using blog.Domain.Common.Enum;
using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Entities;
using blog.Domain.Posts.Enums;
using blog.Domain.Posts.Repository;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Enums;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Posts.Commands
{
    public class CreatePostCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IPostRepository> _postRepositoryMock = new();
        private readonly Mock<IFileStorageService> _fileStorageServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly CreatePostCommandHandler _handler;

        public CreatePostCommandHandlerTests()
        {
            _handler = new CreatePostCommandHandler(
                _userRepositoryMock.Object,
                _postRepositoryMock.Object,
                _fileStorageServiceMock.Object,
                _unitOfWorkMock.Object);
        }

        private static User CreateAuthor(UserLevel level)
        {
            var user = new User("author@test.com", "Ali", "Rezaei", "hashed_password");
            if (level != UserLevel.Normal)
                user.Promote(level);

            return user;
        }

        [Fact]
        public async Task Handle_ValidCommand_ReturnsCreatePostResponse()
        {
            // Arrange
            var author = CreateAuthor(UserLevel.Author);

            var command = new CreatePostCommand
            {
                AuthorId = author.Id.Value,
                Title = "My First Post",
                Content = "Some content",
                Tags = ["dotnet", "ef-core"]
            };

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(command.AuthorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(author);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be(command.Title);
            result.Id.Should().NotBeEmpty();
            result.Slug.Should().Be("my-first-post");
        }

        [Fact]
        public async Task Handle_AuthorNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var command = new CreatePostCommand
            {
                AuthorId = Guid.NewGuid(),
                Title = "My First Post",
                Content = "Some content",
                Tags = ["dotnet"]
            };

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(command.AuthorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_NormalLevelAuthor_ThrowsForbiddenException()
        {
            // Arrange
            var author = CreateAuthor(UserLevel.Normal);

            var command = new CreatePostCommand
            {
                AuthorId = author.Id.Value,
                Title = "My First Post",
                Content = "Some content",
                Tags = ["dotnet"]
            };

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(command.AuthorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(author);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task Handle_AuthorLevelUser_PostStatusIsPendingApproval()
        {
            // Arrange
            var author = CreateAuthor(UserLevel.Author);

            var command = new CreatePostCommand
            {
                AuthorId = author.Id.Value,
                Title = "My First Post",
                Content = "Some content",
                Tags = ["dotnet"]
            };

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(command.AuthorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(author);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Status.Should().Be(PostStatus.PendingApproval);
        }

        [Theory]
        [InlineData(UserLevel.Admin)]
        [InlineData(UserLevel.Owner)]
        public async Task Handle_AdminOrOwnerLevelUser_PostStatusIsPublished(UserLevel level)
        {
            // Arrange
            var author = CreateAuthor(level);

            var command = new CreatePostCommand
            {
                AuthorId = author.Id.Value,
                Title = "My First Post",
                Content = "Some content",
                Tags = ["dotnet"]
            };

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(command.AuthorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(author);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Status.Should().Be(PostStatus.Published);
        }

        [Fact]
        public async Task Handle_DuplicateTitle_ThrowsAlreadyExistsException()
        {
            // Arrange
            var author = CreateAuthor(UserLevel.Author);

            var command = new CreatePostCommand
            {
                AuthorId = author.Id.Value,
                Title = "My First Post",
                Content = "Some content",
                Tags = ["dotnet"]
            };

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(command.AuthorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(author);

            _postRepositoryMock
                .Setup(x => x.ExistsBySlugAsync("my-first-post", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<AlreadyExistsException>();
        }

        [Fact]
        public async Task Handle_NoTitleImageStream_DoesNotCallFileStorageService()
        {
            // Arrange
            var author = CreateAuthor(UserLevel.Author);

            var command = new CreatePostCommand
            {
                AuthorId = author.Id.Value,
                Title = "My First Post",
                Content = "Some content",
                Tags = ["dotnet"],
                TitleImageStream = null
            };

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(command.AuthorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(author);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _fileStorageServiceMock.Verify(
                x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<StorageFolder>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WithTitleImageStream_UploadsToPostsFolder()
        {
            // Arrange
            var author = CreateAuthor(UserLevel.Author);
            using var stream = new MemoryStream();

            var command = new CreatePostCommand
            {
                AuthorId = author.Id.Value,
                Title = "My First Post",
                Content = "Some content",
                Tags = ["dotnet"],
                TitleImageStream = stream,
                TitleImageFileName = "cover.jpg"
            };

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(command.AuthorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(author);

            _fileStorageServiceMock
                .Setup(x => x.UploadAsync(stream, command.TitleImageFileName!, StorageFolder.Posts, It.IsAny<CancellationToken>()))
                .ReturnsAsync("https://cloudinary.com/posts/cover.jpg");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _fileStorageServiceMock.Verify(
                x => x.UploadAsync(stream, command.TitleImageFileName!, StorageFolder.Posts, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_AddsPostToRepository()
        {
            // Arrange
            var author = CreateAuthor(UserLevel.Author);

            var command = new CreatePostCommand
            {
                AuthorId = author.Id.Value,
                Title = "My First Post",
                Content = "Some content",
                Tags = ["dotnet"]
            };

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(command.AuthorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(author);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _postRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<Post>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_SavesChanges()
        {
            // Arrange
            var author = CreateAuthor(UserLevel.Author);

            var command = new CreatePostCommand
            {
                AuthorId = author.Id.Value,
                Title = "My First Post",
                Content = "Some content",
                Tags = ["dotnet"]
            };

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(command.AuthorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(author);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_DeletedAuthor_ThrowsInvalidStateException()
        {
            // Arrange
            var author = CreateAuthor(UserLevel.Author);
            author.SoftDelete();

            var command = new CreatePostCommand
            {
                AuthorId = author.Id.Value,
                Title = "My First Post",
                Content = "Some content",
                Tags = ["dotnet"]
            };

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(command.AuthorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(author);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_BannedAuthor_ThrowsInvalidStateException()
        {
            // Arrange
            var author = CreateAuthor(UserLevel.Author);
            author.Ban();

            var command = new CreatePostCommand
            {
                AuthorId = author.Id.Value,
                Title = "My First Post",
                Content = "Some content",
                Tags = ["dotnet"]
            };

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(command.AuthorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(author);

            // Act
            var act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }
    }
}
