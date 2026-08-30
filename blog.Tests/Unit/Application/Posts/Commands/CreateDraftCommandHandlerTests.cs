using blog.Application.Posts.Commands.CreateDraft;
using blog.Domain.Categories.Entities;
using blog.Domain.Categories.Repository;
using blog.Domain.Categories.Types;
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
    public class CreateDraftCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IPostRepository> _postRepositoryMock = new();
        private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();
        private readonly Mock<IFileStorageService> _fileStorageServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

        private readonly CreateDraftCommandHandler _handler;

        public CreateDraftCommandHandlerTests()
        {
            _handler = new CreateDraftCommandHandler(
                _userRepositoryMock.Object,
                _postRepositoryMock.Object,
                _categoryRepositoryMock.Object,
                _fileStorageServiceMock.Object,
                _unitOfWorkMock.Object);
        }

        private static User CreateUser(
            string email = "author@test.com",
            UserLevel level = UserLevel.Normal)
        {
            var user = new User(
                email,
                "Ali",
                "Rezaei",
                "hashed_password");

            if (level != UserLevel.Normal)
                user.Promote(level);

            return user;
        }

        private static Category CreateCategory()
            => new("Technology");

        private static MemoryStream CreateFakeJpegStream()
        {
            byte[] header =
            [
                0xFF, 0xD8, 0xFF, 0xE0,
                0x00, 0x10,
                0x4A, 0x46, 0x49, 0x46,
                0x00, 0x01
            ];

            return new MemoryStream(header);
        }

        private static CreateDraftCommand CreateCommand(
            Guid authorId,
            Guid categoryId)
        {
            return new CreateDraftCommand
            {
                AuthorId = authorId,
                CategoryId = categoryId,
                Title = "My First Draft",
                Summary = "Draft summary",
                Content = "Draft content",
                Tags = ["dotnet", "csharp"]
            };
        }

        private void SetupUser(User user)
        {
            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    user.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
        }

        private void SetupCategory(CategoryId categoryId)
        {
            _categoryRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    categoryId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateCategory());
        }

        [Fact]
        public async Task Handle_ValidCommand_ReturnsDraftResponse()
        {
            // Arrange
            var author = CreateUser();
            var categoryId = CategoryId.New();
            var command = CreateCommand(
                author.Id.Value,
                categoryId.Value);

            SetupUser(author);
            SetupCategory(categoryId);

            // Act
            var result = await _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().NotBeEmpty();
            result.Title.Should().Be(command.Title);
            result.Summary.Should().Be(command.Summary);
            result.Slug.Should().Be("my-first-draft");
            result.Status.Should().Be(PostStatus.Draft);
        }

        [Fact]
        public async Task Handle_ValidCommand_AddsDraftToRepository()
        {
            // Arrange
            var author = CreateUser();
            var categoryId = CategoryId.New();

            var command = CreateCommand(
                author.Id.Value,
                categoryId.Value);

            SetupUser(author);
            SetupCategory(categoryId);

            // Act
            await _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            _postRepositoryMock.Verify(
                x => x.AddAsync(
                    It.Is<Post>(post =>
                        post.Status == PostStatus.Draft &&
                        post.AuthorId == author.Id &&
                        post.Category.Name == "Technology"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_SavesChanges()
        {
            // Arrange
            var author = CreateUser();
            var categoryId = CategoryId.New();
            var command = CreateCommand(
                author.Id.Value,
                categoryId.Value);

            SetupUser(author);
            SetupCategory(categoryId);

            // Act
            await _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_AuthorNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var authorId = Guid.NewGuid();

            var command = CreateCommand(
                authorId,
                Guid.NewGuid());

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    new UserId(authorId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var act = () => _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_DeletedAuthor_ThrowsInvalidStateException()
        {
            // Arrange
            var author = CreateUser();
            author.SoftDelete();

            var categoryId = CategoryId.New();

            var command = CreateCommand(
                author.Id.Value,
                categoryId.Value);

            SetupUser(author);

            // Act
            var act = () => _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();

            _categoryRepositoryMock.Verify(
                x => x.GetByIdAsync(
                    It.IsAny<CategoryId>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_BannedAuthor_ThrowsInvalidStateException()
        {
            // Arrange
            var author = CreateUser();
            author.Ban();

            var categoryId = CategoryId.New();

            var command = CreateCommand(
                author.Id.Value,
                categoryId.Value);

            SetupUser(author);

            // Act
            var act = () => _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_DraftLimitReached_ThrowsValidationException()
        {
            // Arrange
            var author = CreateUser();
            var categoryId = CategoryId.New();

            var command = CreateCommand(
                author.Id.Value,
                categoryId.Value);

            SetupUser(author);

            _postRepositoryMock
                .Setup(x => x.CountDraftsByAuthorAsync(
                    author.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(10);

            // Act
            var act = () => _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ValidationException>();

            _categoryRepositoryMock.Verify(
                x => x.GetByIdAsync(
                    It.IsAny<CategoryId>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _postRepositoryMock.Verify(
                x => x.AddAsync(
                    It.IsAny<Post>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_CategoryNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var author = CreateUser();
            var categoryId = CategoryId.New();

            var command = CreateCommand(
                author.Id.Value,
                categoryId.Value);

            SetupUser(author);

            _postRepositoryMock
                .Setup(x => x.CountDraftsByAuthorAsync(
                    author.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            _categoryRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    categoryId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Category?)null);

            // Act
            var act = () => _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_DuplicateTitle_ThrowsAlreadyExistsException()
        {
            // Arrange
            var author = CreateUser();
            var categoryId = CategoryId.New();

            var command = CreateCommand(
                author.Id.Value,
                categoryId.Value);

            SetupUser(author);
            SetupCategory(categoryId);

            _postRepositoryMock
                .Setup(x => x.CountDraftsByAuthorAsync(
                    author.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            _postRepositoryMock
                .Setup(x => x.ExistsBySlugAsync(
                    "my-first-draft",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var act = () => _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<AlreadyExistsException>();

            _postRepositoryMock.Verify(
                x => x.AddAsync(
                    It.IsAny<Post>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_UnderDraftLimit_AllowsDraftCreation()
        {
            // Arrange
            var author = CreateUser();
            var categoryId = CategoryId.New();

            var command = CreateCommand(
                author.Id.Value,
                categoryId.Value);

            SetupUser(author);
            SetupCategory(categoryId);

            _postRepositoryMock
                .Setup(x => x.CountDraftsByAuthorAsync(
                    author.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(9);

            // Act
            var result = await _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            result.Status.Should().Be(PostStatus.Draft);

            _postRepositoryMock.Verify(
                x => x.AddAsync(
                    It.IsAny<Post>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_NoTitleImage_DoesNotUploadImage()
        {
            // Arrange
            var author = CreateUser();
            var categoryId = CategoryId.New();

            var command = CreateCommand(
                author.Id.Value,
                categoryId.Value);

            SetupUser(author);
            SetupCategory(categoryId);

            // Act
            await _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            _fileStorageServiceMock.Verify(
                x => x.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<string>(),
                    It.IsAny<StorageFolder>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WithTitleImage_UploadsImageAndStoresUrl()
        {
            // Arrange
            var author = CreateUser();
            var categoryId = CategoryId.New();

            using var stream = CreateFakeJpegStream();

            var command = new CreateDraftCommand
            {
                AuthorId = author.Id.Value,
                CategoryId = categoryId.Value,
                Title = "My First Draft",
                Summary = "Draft summary",
                Content = "Draft content",
                Tags = ["dotnet", "csharp"],
                TitleImageStream = stream,
                TitleImageFileName = "cover.jpg",
                TitleImageContentType = "image/jpeg",
                TitleImageSizeBytes = 500_000
            };

            SetupUser(author);
            SetupCategory(categoryId);

            _fileStorageServiceMock
                .Setup(x => x.UploadAsync(
                    stream,
                    "cover.jpg",
                    StorageFolder.Posts,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync("https://cloudinary.com/posts/cover.webp");

            // Act
            await _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            _fileStorageServiceMock.Verify(
                x => x.UploadAsync(
                    stream,
                    "cover.jpg",
                    StorageFolder.Posts,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _postRepositoryMock.Verify(
                x => x.AddAsync(
                    It.Is<Post>(post =>
                        post.TitleImageUrl == "https://cloudinary.com/posts/cover.webp"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_PreservesTags()
        {
            // Arrange
            var author = CreateUser();
            var categoryId = CategoryId.New();

            var command = CreateCommand(
                author.Id.Value,
                categoryId.Value);

            SetupUser(author);
            SetupCategory(categoryId);

            // Act
            await _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            _postRepositoryMock.Verify(
                x => x.AddAsync(
                    It.Is<Post>(post =>
                        post.Tags.SequenceEqual(command.Tags)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}