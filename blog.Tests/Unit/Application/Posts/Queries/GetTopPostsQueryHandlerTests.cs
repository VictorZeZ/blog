using blog.Application.Posts.Queries.GetTopPosts;
using blog.Domain.Categories.Entities;
using blog.Domain.Categories.Repository;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Entities;
using blog.Domain.Posts.Repository;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Enums;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Posts.Queries
{
    public class GetTopPostsQueryHandlerTests
    {
        private readonly Mock<IPostRepository> _postRepositoryMock = new();
        private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();
        private readonly GetTopPostsQueryHandler _handler;

        public GetTopPostsQueryHandlerTests()
        {
            _handler = new GetTopPostsQueryHandler(
                _postRepositoryMock.Object,
                _categoryRepositoryMock.Object);
        }

        private static User CreateAuthor()
        {
            var user = new User(
                "author@test.com",
                "Ali",
                "Rezaei",
                "hashed_password");

            user.Promote(UserLevel.Author);

            return user;
        }

        private static Post CreatePost(
            string title = "Test Post",
            string summary = "Test Summary")
        {
            return new Post(
                title,
                summary,
                null,
                "Test content",
                ["dotnet"],
                CreateAuthor(),
                new Category("Technology"));
        }

        [Fact]
        public async Task Handle_ValidQuery_ReturnsTopPosts()
        {
            // Arrange
            var from = new DateOnly(2026, 8, 1);
            var to = new DateOnly(2026, 8, 30);

            var query = new GetTopPostsQuery
            {
                ActorId = Guid.NewGuid(),
                From = from,
                To = to,
                TopN = 5
            };

            var posts = new List<Post>
            {
                CreatePost("Most Viewed Post", "Most viewed summary"),
                CreatePost("Second Post", "Second summary")
            };

            _postRepositoryMock
                .Setup(x => x.GetTopViewedAsync(
                    from,
                    to,
                    5,
                    null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(posts);

            // Act
            var result = await _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);

            result[0].Title.Should().Be("Most Viewed Post");
            result[0].Summary.Should().Be("Most viewed summary");

            result[1].Title.Should().Be("Second Post");
            result[1].Summary.Should().Be("Second summary");
        }

        [Fact]
        public async Task Handle_ValidQuery_PassesDateRangeAndTopNToRepository()
        {
            // Arrange
            var from = new DateOnly(2026, 8, 1);
            var to = new DateOnly(2026, 8, 30);

            var query = new GetTopPostsQuery
            {
                ActorId = Guid.NewGuid(),
                From = from,
                To = to,
                TopN = 10
            };

            _postRepositoryMock
                .Setup(x => x.GetTopViewedAsync(
                    from,
                    to,
                    10,
                    null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            await _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            _postRepositoryMock.Verify(
                x => x.GetTopViewedAsync(
                    from,
                    to,
                    10,
                    null,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidQuery_WithCategory_PassesCategoryToRepository()
        {
            // Arrange
            var from = new DateOnly(2026, 8, 1);
            var to = new DateOnly(2026, 8, 30);
            var categoryId = Guid.NewGuid();

            var query = new GetTopPostsQuery
            {
                ActorId = Guid.NewGuid(),
                From = from,
                To = to,
                TopN = 5,
                CategoryId = categoryId
            };

            var category = new Category("Technology");

            _categoryRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    It.Is<blog.Domain.Categories.Types.CategoryId>(
                        id => id.Value == categoryId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(category);

            _postRepositoryMock
                .Setup(x => x.GetTopViewedAsync(
                    from,
                    to,
                    5,
                    It.Is<blog.Domain.Categories.Types.CategoryId?>(
                        id => id.HasValue && id.Value.Value == categoryId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            await _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            _categoryRepositoryMock.Verify(
                x => x.GetByIdAsync(
                    It.Is<blog.Domain.Categories.Types.CategoryId>(
                        id => id.Value == categoryId),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _postRepositoryMock.Verify(
                x => x.GetTopViewedAsync(
                    from,
                    to,
                    5,
                    It.Is<blog.Domain.Categories.Types.CategoryId?>(
                        id => id.HasValue && id.Value.Value == categoryId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_CategoryDoesNotExist_ThrowsNotFoundException()
        {
            // Arrange
            var categoryId = Guid.NewGuid();

            var query = new GetTopPostsQuery
            {
                ActorId = Guid.NewGuid(),
                CategoryId = categoryId
            };

            _categoryRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    It.Is<blog.Domain.Categories.Types.CategoryId>(
                        id => id.Value == categoryId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Category?)null);

            // Act
            var act = () => _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();

            _postRepositoryMock.Verify(
                x => x.GetTopViewedAsync(
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<int>(),
                    It.IsAny<blog.Domain.Categories.Types.CategoryId?>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_RepositoryReturnsEmptyList_ReturnsEmptyResult()
        {
            // Arrange
            var from = new DateOnly(2026, 8, 1);
            var to = new DateOnly(2026, 8, 30);

            var query = new GetTopPostsQuery
            {
                ActorId = Guid.NewGuid(),
                From = from,
                To = to,
                TopN = 5
            };

            _postRepositoryMock
                .Setup(x => x.GetTopViewedAsync(
                    from,
                    to,
                    5,
                    null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            var result = await _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
    }
}