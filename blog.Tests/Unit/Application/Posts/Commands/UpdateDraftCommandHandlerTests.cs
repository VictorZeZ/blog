using blog.Application.Posts.Commands.UpdateDraft;
using blog.Domain.Categories.Entities;
using blog.Domain.Categories.Repository;
using blog.Domain.Categories.Types;
using blog.Domain.Common.Enum;
using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Entities;
using blog.Domain.Posts.Enums;
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
    public class UpdateDraftCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IPostRepository> _postRepositoryMock = new();
        private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();
        private readonly Mock<IFileStorageService> _fileStorageServiceMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

        private readonly UpdateDraftCommandHandler _handler;

        public UpdateDraftCommandHandlerTests()
        {
            _handler = new UpdateDraftCommandHandler(
                _userRepositoryMock.Object,
                _postRepositoryMock.Object,
                _categoryRepositoryMock.Object,
                _fileStorageServiceMock.Object,
                _unitOfWorkMock.Object);
        }

        private static User CreateUser(
            string email,
            UserLevel level)
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

        private static Post CreateDraft(User author)
        {
            return Post.CreateDraft(
                "Old Draft Title",
                "Old summary",
                null,
                "Old content",
                ["dotnet"],
                author,
                new Category("Technology"));
        }

        private static Post CreateDraftWithImage(User author)
        {
            return Post.CreateDraft(
                "Old Draft Title",
                "Old summary",
                "https://cloudinary.com/posts/old.webp",
                "Old content",
                ["dotnet"],
                author,
                new Category("Technology"));
        }

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

        private static Category CreateCategory(string name = "Technology")
            => new(name);

        private static UpdateDraftCommand CreateCommand(
            User actor,
            Post post,
            Guid categoryId)
        {
            return new UpdateDraftCommand
            {
                ActorId = actor.Id.Value,
                PostId = post.Id.Value,
                CategoryId = categoryId,
                Title = "Updated Draft Title",
                Summary = "Updated summary",
                Content = "Updated content",
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

        private void SetupPost(Post post)
        {
            _postRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    post.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(post);
        }

        private void SetupCategory(Guid categoryId)
        {
            _categoryRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    new CategoryId(categoryId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateCategory());
        }

        [Fact]
        public async Task Handle_ValidCommand_UpdatesDraft()
        {
            // Arrange
            var actor = CreateUser(
                "author@test.com",
                UserLevel.Normal);

            var post = CreateDraft(actor);
            var categoryId = Guid.NewGuid();

            var command = CreateCommand(
                actor,
                post,
                categoryId);

            SetupUser(actor);
            SetupPost(post);
            SetupCategory(categoryId);

            // Act
            var result = await _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(post.Id.Value);
            result.Title.Should().Be("Updated Draft Title");
            result.Summary.Should().Be("Updated summary");
            result.Slug.Should().Be("updated-draft-title");
            result.Status.Should().Be(PostStatus.Draft);

            post.Title.Should().Be("Updated Draft Title");
            post.Summary.Should().Be("Updated summary");
            post.Content.Should().Be("Updated content");
            post.Tags.Should().BeEquivalentTo(["dotnet", "csharp"]);
            post.Status.Should().Be(PostStatus.Draft);
        }

        [Fact]
        public async Task Handle_ValidCommand_UpdatesRepository()
        {
            // Arrange
            var actor = CreateUser(
                "author@test.com",
                UserLevel.Normal);

            var post = CreateDraft(actor);
            var categoryId = Guid.NewGuid();

            var command = CreateCommand(
                actor,
                post,
                categoryId);

            SetupUser(actor);
            SetupPost(post);
            SetupCategory(categoryId);

            // Act
            await _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            _postRepositoryMock.Verify(
                x => x.Update(post),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_SavesChanges()
        {
            // Arrange
            var actor = CreateUser(
                "author@test.com",
                UserLevel.Normal);

            var post = CreateDraft(actor);
            var categoryId = Guid.NewGuid();

            var command = CreateCommand(
                actor,
                post,
                categoryId);

            SetupUser(actor);
            SetupPost(post);
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
        public async Task Handle_ActorNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var actorId = Guid.NewGuid();

            var command = new UpdateDraftCommand
            {
                ActorId = actorId,
                PostId = Guid.NewGuid(),
                CategoryId = Guid.NewGuid(),
                Title = "Updated",
                Summary = "Summary",
                Content = "Content",
                Tags = ["dotnet"]
            };

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    new UserId(actorId),
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
        public async Task Handle_DeletedActor_ThrowsInvalidStateException()
        {
            // Arrange
            var actor = CreateUser(
                "actor@test.com",
                UserLevel.Author);

            actor.SoftDelete();

            var command = new UpdateDraftCommand
            {
                ActorId = actor.Id.Value,
                PostId = Guid.NewGuid(),
                CategoryId = Guid.NewGuid(),
                Title = "Updated",
                Summary = "Summary",
                Content = "Content",
                Tags = ["dotnet"]
            };

            SetupUser(actor);

            // Act
            var act = () => _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_BannedActor_ThrowsInvalidStateException()
        {
            // Arrange
            var actor = CreateUser(
                "actor@test.com",
                UserLevel.Author);

            actor.Ban();

            var command = new UpdateDraftCommand
            {
                ActorId = actor.Id.Value,
                PostId = Guid.NewGuid(),
                CategoryId = Guid.NewGuid(),
                Title = "Updated",
                Summary = "Summary",
                Content = "Content",
                Tags = ["dotnet"]
            };

            SetupUser(actor);

            // Act
            var act = () => _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_PostNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var actor = CreateUser(
                "actor@test.com",
                UserLevel.Author);

            var postId = Guid.NewGuid();

            var command = new UpdateDraftCommand
            {
                ActorId = actor.Id.Value,
                PostId = postId,
                CategoryId = Guid.NewGuid(),
                Title = "Updated",
                Summary = "Summary",
                Content = "Content",
                Tags = ["dotnet"]
            };

            SetupUser(actor);

            _postRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    new PostId(postId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Post?)null);

            // Act
            var act = () => _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_UnrelatedNormalActor_ThrowsForbiddenException()
        {
            // Arrange
            var author = CreateUser(
                "author@test.com",
                UserLevel.Normal);

            var actor = CreateUser(
                "other@test.com",
                UserLevel.Normal);

            var post = CreateDraft(author);

            var command = new UpdateDraftCommand
            {
                ActorId = actor.Id.Value,
                PostId = post.Id.Value,
                CategoryId = Guid.NewGuid(),
                Title = "Updated",
                Summary = "Summary",
                Content = "Content",
                Tags = ["dotnet"]
            };

            SetupUser(actor);
            SetupPost(post);

            // Act
            var act = () => _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task Handle_ElevatedActor_CanUpdateAnotherUsersDraft()
        {
            // Arrange
            var author = CreateUser(
                "author@test.com",
                UserLevel.Normal);

            var admin = CreateUser(
                "admin@test.com",
                UserLevel.Admin);

            var post = CreateDraft(author);
            var categoryId = Guid.NewGuid();

            var command = CreateCommand(
                admin,
                post,
                categoryId);

            SetupUser(admin);
            SetupPost(post);
            SetupCategory(categoryId);

            // Act
            var result = await _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            result.Status.Should().Be(PostStatus.Draft);
            result.Title.Should().Be(command.Title);
        }

        [Fact]
        public async Task Handle_PublishedPost_ThrowsInvalidStateException()
        {
            // Arrange
            var actor = CreateUser(
                "author@test.com",
                UserLevel.Author);

            var post = new Post(
                "Published Post",
                "Summary",
                null,
                "Content",
                ["dotnet"],
                actor,
                new Category("Technology"));

            var command = new UpdateDraftCommand
            {
                ActorId = actor.Id.Value,
                PostId = post.Id.Value,
                CategoryId = Guid.NewGuid(),
                Title = "Updated",
                Summary = "Summary",
                Content = "Content",
                Tags = ["dotnet"]
            };

            SetupUser(actor);
            SetupPost(post);

            // Act
            var act = () => _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_CategoryNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var actor = CreateUser(
                "author@test.com",
                UserLevel.Normal);

            var post = CreateDraft(actor);
            var categoryId = Guid.NewGuid();

            var command = CreateCommand(
                actor,
                post,
                categoryId);

            SetupUser(actor);
            SetupPost(post);

            _categoryRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    new CategoryId(categoryId),
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
        public async Task Handle_SameSlug_DoesNotCheckSlugUniqueness()
        {
            // Arrange
            var actor = CreateUser(
                "author@test.com",
                UserLevel.Normal);

            var post = CreateDraft(actor);
            var categoryId = Guid.NewGuid();

            var command = new UpdateDraftCommand
            {
                ActorId = actor.Id.Value,
                PostId = post.Id.Value,
                CategoryId = categoryId,
                Title = post.Title,
                Summary = "Updated summary",
                Content = "Updated content",
                Tags = ["dotnet"]
            };

            SetupUser(actor);
            SetupPost(post);
            SetupCategory(categoryId);

            // Act
            await _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            _postRepositoryMock.Verify(
                x => x.ExistsBySlugAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_TitleChangedToTakenSlug_ThrowsAlreadyExistsException()
        {
            // Arrange
            var actor = CreateUser(
                "author@test.com",
                UserLevel.Normal);

            var post = CreateDraft(actor);
            var categoryId = Guid.NewGuid();

            var command = new UpdateDraftCommand
            {
                ActorId = actor.Id.Value,
                PostId = post.Id.Value,
                CategoryId = categoryId,
                Title = "Taken Title",
                Summary = "Updated summary",
                Content = "Updated content",
                Tags = ["dotnet"]
            };

            SetupUser(actor);
            SetupPost(post);
            SetupCategory(categoryId);

            _postRepositoryMock
                .Setup(x => x.ExistsBySlugAsync(
                    "taken-title",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var act = () => _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<AlreadyExistsException>();

            _postRepositoryMock.Verify(
                x => x.Update(It.IsAny<Post>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_TitleChangedToAvailableSlug_UpdatesSlug()
        {
            // Arrange
            var actor = CreateUser(
                "author@test.com",
                UserLevel.Normal);

            var post = CreateDraft(actor);
            var categoryId = Guid.NewGuid();

            var command = new UpdateDraftCommand
            {
                ActorId = actor.Id.Value,
                PostId = post.Id.Value,
                CategoryId = categoryId,
                Title = "New Available Title",
                Summary = "Updated summary",
                Content = "Updated content",
                Tags = ["dotnet"]
            };

            SetupUser(actor);
            SetupPost(post);
            SetupCategory(categoryId);

            _postRepositoryMock
                .Setup(x => x.ExistsBySlugAsync(
                    "new-available-title",
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            result.Slug.Should().Be("new-available-title");

            _postRepositoryMock.Verify(
                x => x.ExistsBySlugAsync(
                    "new-available-title",
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_NoImageChange_KeepsExistingImage()
        {
            // Arrange
            var actor = CreateUser(
                "author@test.com",
                UserLevel.Normal);

            var post = CreateDraftWithImage(actor);
            var categoryId = Guid.NewGuid();

            var command = CreateCommand(
                actor,
                post,
                categoryId);

            SetupUser(actor);
            SetupPost(post);
            SetupCategory(categoryId);

            // Act
            var result = await _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            result.TitleImageUrl.Should()
                .Be("https://cloudinary.com/posts/old.webp");

            _fileStorageServiceMock.Verify(
                x => x.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<string>(),
                    It.IsAny<StorageFolder>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _fileStorageServiceMock.Verify(
                x => x.DeleteAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_RemoveTitleImage_DeletesExistingImage()
        {
            // Arrange
            var actor = CreateUser(
                "author@test.com",
                UserLevel.Normal);

            var post = CreateDraftWithImage(actor);
            var categoryId = Guid.NewGuid();

            var command = new UpdateDraftCommand
            {
                ActorId = actor.Id.Value,
                PostId = post.Id.Value,
                CategoryId = categoryId,
                Title = "Updated Draft Title",
                Summary = "Updated summary",
                Content = "Updated content",
                Tags = ["dotnet"],
                RemoveTitleImage = true
            };

            SetupUser(actor);
            SetupPost(post);
            SetupCategory(categoryId);

            // Act
            var result = await _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            result.TitleImageUrl.Should().BeNull();

            _fileStorageServiceMock.Verify(
                x => x.DeleteAsync(
                    "https://cloudinary.com/posts/old.webp",
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _fileStorageServiceMock.Verify(
                x => x.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<string>(),
                    It.IsAny<StorageFolder>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ReplaceTitleImage_UploadsNewAndDeletesOld()
        {
            // Arrange
            var actor = CreateUser(
                "author@test.com",
                UserLevel.Normal);

            var post = CreateDraftWithImage(actor);
            var categoryId = Guid.NewGuid();

            using var stream = CreateFakeJpegStream();

            var command = new UpdateDraftCommand
            {
                ActorId = actor.Id.Value,
                PostId = post.Id.Value,
                CategoryId = categoryId,
                Title = "Updated Draft Title",
                Summary = "Updated summary",
                Content = "Updated content",
                Tags = ["dotnet"],
                TitleImageStream = stream,
                TitleImageFileName = "new-cover.jpg",
                TitleImageContentType = "image/jpeg",
                TitleImageSizeBytes = 500_000
            };

            SetupUser(actor);
            SetupPost(post);
            SetupCategory(categoryId);

            _fileStorageServiceMock
                .Setup(x => x.UploadAsync(
                    stream,
                    "new-cover.jpg",
                    StorageFolder.Posts,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync("https://cloudinary.com/posts/new.webp");

            // Act
            var result = await _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            result.TitleImageUrl.Should()
                .Be("https://cloudinary.com/posts/new.webp");

            _fileStorageServiceMock.Verify(
                x => x.UploadAsync(
                    stream,
                    "new-cover.jpg",
                    StorageFolder.Posts,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _fileStorageServiceMock.Verify(
                x => x.DeleteAsync(
                    "https://cloudinary.com/posts/old.webp",
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_NewImageWithoutExistingImage_DoesNotDeleteImage()
        {
            // Arrange
            var actor = CreateUser(
                "author@test.com",
                UserLevel.Normal);

            var post = CreateDraft(actor);
            var categoryId = Guid.NewGuid();

            using var stream = CreateFakeJpegStream();

            var command = new UpdateDraftCommand
            {
                ActorId = actor.Id.Value,
                PostId = post.Id.Value,
                CategoryId = categoryId,
                Title = "Updated Draft Title",
                Summary = "Updated summary",
                Content = "Updated content",
                Tags = ["dotnet"],
                TitleImageStream = stream,
                TitleImageFileName = "cover.jpg",
                TitleImageContentType = "image/jpeg",
                TitleImageSizeBytes = 500_000
            };

            SetupUser(actor);
            SetupPost(post);
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

            _fileStorageServiceMock.Verify(
                x => x.DeleteAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_RemoveAndReplaceImage_UsesRemovePath()
        {
            // Arrange
            var actor = CreateUser(
                "author@test.com",
                UserLevel.Normal);

            var post = CreateDraftWithImage(actor);
            var categoryId = Guid.NewGuid();

            using var stream = CreateFakeJpegStream();

            var command = new UpdateDraftCommand
            {
                ActorId = actor.Id.Value,
                PostId = post.Id.Value,
                CategoryId = categoryId,
                Title = "Updated Draft Title",
                Summary = "Updated summary",
                Content = "Updated content",
                Tags = ["dotnet"],
                TitleImageStream = stream,
                TitleImageFileName = "cover.jpg",
                TitleImageContentType = "image/jpeg",
                TitleImageSizeBytes = 500_000,
                RemoveTitleImage = true
            };

            SetupUser(actor);
            SetupPost(post);
            SetupCategory(categoryId);

            _fileStorageServiceMock
                .Setup(x => x.UploadAsync(
                    stream,
                    "cover.jpg",
                    StorageFolder.Posts,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync("https://cloudinary.com/posts/new.webp");

            // Act
            await _handler.Handle(
                command,
                CancellationToken.None);

            // Assert
            _fileStorageServiceMock.Verify(
                x => x.DeleteAsync(
                    "https://cloudinary.com/posts/old.webp",
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _fileStorageServiceMock.Verify(
                x => x.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<string>(),
                    It.IsAny<StorageFolder>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}