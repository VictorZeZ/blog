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
            var user = new User(
                email,
                "Ali",
                "Rezaei",
                "hashed_password");

            if (level != UserLevel.Normal)
                user.Promote(level);

            return user;
        }

        private static Post CreatePublishedPost()
        {
            var author = CreateUser(
                "author@test.com",
                UserLevel.Author);

            return new Post(
                "My First Post",
                "Summary for post",
                null,
                "Some content",
                ["dotnet"],
                author,
                new Category("Technology"));
        }

        private static Post CreatePendingPost()
        {
            var author = CreateUser(
                "author@test.com",
                UserLevel.Normal);

            return new Post(
                "My First Post",
                "Summary for post",
                null,
                "Some content",
                ["dotnet"],
                author,
                new Category("Technology"));
        }

        private void SetupPost(Post post)
        {
            _postRepositoryMock
                .Setup(x => x.GetBySlugAsync(
                    post.Slug,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(post);
        }

        [Fact]
        public async Task Handle_PublishedPost_ReturnsResponse()
        {
            // Arrange
            var post = CreatePublishedPost();
            var query = new GetPostBySlugQuery
            {
                Slug = post.Slug
            };

            SetupPost(post);

            // Act
            var result = await _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(post.Id.Value);
            result.Title.Should().Be(post.Title);
            result.Slug.Should().Be(post.Slug);
            result.Status.Should().Be(PostStatus.Published);
            result.AuthorId.Should().Be(post.AuthorId.Value);
            result.AuthorFullName.Should().Be(post.Author.FullName);
            result.CategoryName.Should().Be(post.Category.Name);
            result.Tags.Should().BeEquivalentTo(post.Tags);
        }

        [Fact]
        public async Task Handle_PostNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var query = new GetPostBySlugQuery
            {
                Slug = "non-existent-slug"
            };

            _postRepositoryMock
                .Setup(x => x.GetBySlugAsync(
                    query.Slug,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Post?)null);

            // Act
            var act = () => _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_PublishedPost_IncrementsViewCountAndSavesChanges()
        {
            // Arrange
            var post = CreatePublishedPost();
            var query = new GetPostBySlugQuery
            {
                Slug = post.Slug
            };

            SetupPost(post);

            // Act
            var result = await _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            result.ViewCount.Should().Be(1);

            _postRepositoryMock.Verify(
                x => x.Update(post),
                Times.Once);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_UnpublishedPostWithNoActor_ThrowsNotFoundException()
        {
            // Arrange
            var post = CreatePendingPost();

            var query = new GetPostBySlugQuery
            {
                Slug = post.Slug,
                ActorId = null
            };

            SetupPost(post);

            // Act
            var act = () => _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_UnpublishedPostWithActorNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var post = CreatePendingPost();
            var actorId = Guid.NewGuid();

            var query = new GetPostBySlugQuery
            {
                Slug = post.Slug,
                ActorId = actorId
            };

            SetupPost(post);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    new UserId(actorId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var act = () => _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_UnpublishedPostWithUnrelatedNormalActor_ThrowsNotFoundException()
        {
            // Arrange
            var post = CreatePendingPost();
            var otherActor = CreateUser(
                "other@test.com",
                UserLevel.Normal);

            var query = new GetPostBySlugQuery
            {
                Slug = post.Slug,
                ActorId = otherActor.Id.Value
            };

            SetupPost(post);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    otherActor.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(otherActor);

            // Act
            var act = () => _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_UnpublishedPostWithOwnerActor_ReturnsResponse()
        {
            // Arrange
            var post = CreatePendingPost();

            var owner = CreateUser(
                "owner@test.com",
                UserLevel.Owner);

            var query = new GetPostBySlugQuery
            {
                Slug = post.Slug,
                ActorId = owner.Id.Value
            };

            SetupPost(post);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    owner.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(owner);

            // Act
            var result = await _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(PostStatus.PendingApproval);
            result.Id.Should().Be(post.Id.Value);
            result.ViewCount.Should().Be(0);
        }

        [Theory]
        [InlineData(UserLevel.Admin)]
        [InlineData(UserLevel.Owner)]
        public async Task Handle_UnpublishedPostWithElevatedNonOwnerActor_ReturnsResponse(
            UserLevel actorLevel)
        {
            // Arrange
            var post = CreatePendingPost();

            var elevatedActor = CreateUser(
                "moderator@test.com",
                actorLevel);

            var query = new GetPostBySlugQuery
            {
                Slug = post.Slug,
                ActorId = elevatedActor.Id.Value
            };

            SetupPost(post);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    elevatedActor.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(elevatedActor);

            // Act
            var result = await _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(PostStatus.PendingApproval);
            result.Id.Should().Be(post.Id.Value);
        }

        [Fact]
        public async Task Handle_UnpublishedPostPreviewedByOwner_DoesNotIncrementViewCount()
        {
            // Arrange
            var post = CreatePendingPost();

            var owner = CreateUser(
                "owner@test.com",
                UserLevel.Owner);

            var query = new GetPostBySlugQuery
            {
                Slug = post.Slug,
                ActorId = owner.Id.Value
            };

            SetupPost(post);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    owner.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(owner);

            // Act
            var result = await _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            result.ViewCount.Should().Be(0);

            _postRepositoryMock.Verify(
                x => x.Update(It.IsAny<Post>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}