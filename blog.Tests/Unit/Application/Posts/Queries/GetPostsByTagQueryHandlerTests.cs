using blog.Application.Posts.Queries.GetPostsByTag;
using blog.Domain.Categories.Entities;
using blog.Domain.Common;
using blog.Domain.Posts.Entities;
using blog.Domain.Posts.Enums;
using blog.Domain.Posts.Repository;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Enums;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Posts.Queries
{
    public class GetPostsByTagQueryHandlerTests
    {
        private readonly Mock<IPostRepository> _postRepositoryMock = new();
        private readonly GetPostsByTagQueryHandler _handler;

        public GetPostsByTagQueryHandlerTests()
        {
            _handler = new GetPostsByTagQueryHandler(
                _postRepositoryMock.Object);
        }

        private static User CreateUser(UserLevel level)
        {
            var user = new User(
                "author@test.com",
                "Ali",
                "Rezaei",
                "hashed_password");

            if (level != UserLevel.Normal)
                user.Promote(level);

            return user;
        }

        private static GetPostsByTagQuery ValidQuery => new()
        {
            Tags = ["dotnet", "asp", "csharp"],
            Paging = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            },
            SortBy = PostSortBy.Newest,
            GroupingMode = PostTagGroupingMode.None
        };

        private static PagedResult<Post> EmptyPagedResult =>
            new([], 0, 1, 10);

        [Fact]
        public async Task Handle_ValidQuery_ReturnsPagedResult()
        {
            // Arrange
            var query = ValidQuery;

            _postRepositoryMock
                .Setup(x => x.GetByTagAsync(
                    query.Paging,
                    query.Tags,
                    query.SortBy,
                    query.GroupingMode,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmptyPagedResult);

            // Act
            var result = await _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_ValidQuery_ReturnsCorrectPostData()
        {
            // Arrange
            var query = ValidQuery;
            var author = CreateUser(UserLevel.Admin);

            var posts = new List<Post>
            {
                new(
                    "Dotnet Post",
                    "Summary for post",
                    null,
                    "Content",
                    ["dotnet", "ef-core"],
                    author,
                    new Category("Technology"))
            };

            var pagedResult = new PagedResult<Post>(
                posts,
                1,
                1,
                10);

            _postRepositoryMock
                .Setup(x => x.GetByTagAsync(
                    query.Paging,
                    query.Tags,
                    query.SortBy,
                    query.GroupingMode,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(1);

            var item = result.Items.First();

            item.Title.Should().Be("Dotnet Post");
            item.Summary.Should().Be("Summary for post");
            item.Tags.Should().Contain("dotnet");
            item.Tags.Should().Contain("ef-core");
        }

        [Fact]
        public async Task Handle_ValidQuery_PassesTagsAndGroupingModeToRepository()
        {
            // Arrange
            var query = ValidQuery;

            _postRepositoryMock
                .Setup(x => x.GetByTagAsync(
                    query.Paging,
                    query.Tags,
                    query.SortBy,
                    query.GroupingMode,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmptyPagedResult);

            // Act
            await _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            _postRepositoryMock.Verify(
                x => x.GetByTagAsync(
                    query.Paging,
                    query.Tags,
                    query.SortBy,
                    query.GroupingMode,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}