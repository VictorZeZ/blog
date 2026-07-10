using blog.Application.Posts.Queries.GetPostBySlug;
using blog.Domain.Categories.Entities;
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

namespace blog.Tests.Unit.Application.Posts.Queries
{
    public class GetPostBySlugQueryHandlerTests
    {
        private readonly Mock<IPostRepository> _postRepositoryMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly GetPostBySlugQueryHandler _handler;

        public GetPostBySlugQueryHandlerTests()
        {
            _handler = new GetPostBySlugQueryHandler(
                _postRepositoryMock.Object,
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

        private static Post CreatePublishedPost(User author)
        {
            // Admin/Owner authored posts are auto-published per Post constructor rules
            var post = new Post("My First Post", null, "Some content", ["dotnet"], author, new Category("Technology"));
            return post;
        }

        private static Post CreatePendingPost(User author)
            => new("My First Post", null, "Some content", ["dotnet"], author, new Category("Technology"));

        private void SetupPost(Post post)
        {
            _postRepositoryMock
                .Setup(x => x.GetBySlugAsync(post.Slug, It.IsAny<CancellationToken>()))
                .ReturnsAsync(post);
        }

        [Fact]
        public async Task Handle_PublishedPost_ReturnsResponse()
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Admin);
            var post = CreatePublishedPost(author);
            var query = new GetPostBySlugQuery { Slug = post.Slug };

            SetupPost(post);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(post.Id.Value);
            result.Title.Should().Be(post.Title);
            result.Slug.Should().Be(post.Slug);
            result.Status.Should().Be(PostStatus.Published);
            result.AuthorId.Should().Be(author.Id.Value);
            result.AuthorFullName.Should().Be(author.FullName);
            result.CategoryName.Should().Be(post.Category.Name);
        }

        [Fact]
        public async Task Handle_PostNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var query = new GetPostBySlugQuery { Slug = "non-existent-slug" };

            _postRepositoryMock
                .Setup(x => x.GetBySlugAsync(query.Slug, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Post?)null);

            // Act
            var act = () => _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_PublishedPost_IncrementsViewCountAndSavesChanges()
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Admin);
            var post = CreatePublishedPost(author);
            var query = new GetPostBySlugQuery { Slug = post.Slug };

            SetupPost(post);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.ViewCount.Should().Be(1);

            _postRepositoryMock.Verify(
                x => x.Update(post),
                Times.Once);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_UnpublishedPostWithNoActor_ThrowsNotFoundException()
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Author);
            var post = CreatePendingPost(author);
            var query = new GetPostBySlugQuery { Slug = post.Slug, ActorId = null };

            SetupPost(post);

            // Act
            var act = () => _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_UnpublishedPostWithActorNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Author);
            var post = CreatePendingPost(author);
            var query = new GetPostBySlugQuery { Slug = post.Slug, ActorId = Guid.NewGuid() };

            SetupPost(post);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(query.ActorId!.Value), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var act = () => _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_UnpublishedPostWithUnrelatedNormalActor_ThrowsNotFoundException()
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Author);
            var post = CreatePendingPost(author);
            var otherActor = CreateUser("other@test.com", UserLevel.Normal);
            var query = new GetPostBySlugQuery { Slug = post.Slug, ActorId = otherActor.Id.Value };

            SetupPost(post);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(otherActor.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(otherActor);

            // Act
            var act = () => _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_UnpublishedPostWithOwnerActor_ReturnsResponse()
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Author);
            var post = CreatePendingPost(author);
            var query = new GetPostBySlugQuery { Slug = post.Slug, ActorId = author.Id.Value };

            SetupPost(post);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(author.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(author);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(PostStatus.PendingApproval);
        }

        [Theory]
        [InlineData(UserLevel.Admin)]
        [InlineData(UserLevel.Owner)]
        public async Task Handle_UnpublishedPostWithElevatedNonOwnerActor_ReturnsResponse(UserLevel actorLevel)
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Author);
            var post = CreatePendingPost(author);
            var elevatedActor = CreateUser("moderator@test.com", actorLevel);
            var query = new GetPostBySlugQuery { Slug = post.Slug, ActorId = elevatedActor.Id.Value };

            SetupPost(post);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(elevatedActor.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(elevatedActor);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(PostStatus.PendingApproval);
        }

        [Fact]
        public async Task Handle_UnpublishedPostPreviewedByOwner_DoesNotIncrementViewCount()
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Author);
            var post = CreatePendingPost(author);
            var query = new GetPostBySlugQuery { Slug = post.Slug, ActorId = author.Id.Value };

            SetupPost(post);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(author.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(author);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.ViewCount.Should().Be(0);

            _postRepositoryMock.Verify(
                x => x.Update(It.IsAny<Post>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
