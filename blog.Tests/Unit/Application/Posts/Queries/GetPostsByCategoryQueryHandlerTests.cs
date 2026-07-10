using blog.Application.Posts.Queries.GetPostsByCategory;
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
    public class GetPostsByCategoryQueryHandlerTests
    {
        private readonly Mock<IPostRepository> _postRepositoryMock = new();
        private readonly GetPostsByCategoryQueryHandler _handler;

        public GetPostsByCategoryQueryHandlerTests()
        {
            _handler = new GetPostsByCategoryQueryHandler(_postRepositoryMock.Object);
        }

        private static User CreateAuthor(UserLevel level)
        {
            var user = new User("author@test.com", "Ali", "Rezaei", "hashed_password");
            if (level != UserLevel.Normal)
                user.Promote(level);

            return user;
        }

        private static GetPostsByCategoryQuery ValidQuery => new()
        {
            CategorySlug = "technology",
            Paging = new PagedRequest { Page = 1, PageSize = 10 },
            SortBy = PostSortBy.Newest
        };

        private static PagedResult<Post> EmptyPagedResult => new([], 0, 1, 10);

        [Fact]
        public async Task Handle_ValidQuery_ReturnsPagedResult()
        {
            // Arrange
            var query = ValidQuery;

            _postRepositoryMock
                .Setup(x => x.GetByCategorySlugAsync(query.Paging, query.CategorySlug, query.SortBy, It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmptyPagedResult);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_ValidQuery_ReturnsCorrectPostData()
        {
            // Arrange
            var query = ValidQuery;
            var author = CreateAuthor(UserLevel.Admin);
            var category = new Category("Technology");
            var posts = new List<Post> { new("EF Core Migrations Guide", null, "Content", ["dotnet"], author, category) };
            var pagedResult = new PagedResult<Post>(posts, 1, 1, 10);

            _postRepositoryMock
                .Setup(x => x.GetByCategorySlugAsync(query.Paging, query.CategorySlug, query.SortBy, It.IsAny<CancellationToken>()))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items.First().Title.Should().Be("EF Core Migrations Guide");
            result.Items.First().CategoryName.Should().Be(category.Name);
        }

        [Fact]
        public async Task Handle_NoMatchingPosts_ReturnsEmptyResult()
        {
            // Arrange
            var query = new GetPostsByCategoryQuery
            {
                CategorySlug = "non-existent-category",
                Paging = new PagedRequest { Page = 1, PageSize = 10 },
                SortBy = PostSortBy.Newest
            };

            _postRepositoryMock
                .Setup(x => x.GetByCategorySlugAsync(query.Paging, query.CategorySlug, query.SortBy, It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmptyPagedResult);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.TotalCount.Should().Be(0);
            result.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_ValidQuery_PassesCategorySlugToRepository()
        {
            // Arrange
            var query = ValidQuery;

            _postRepositoryMock
                .Setup(x => x.GetByCategorySlugAsync(query.Paging, query.CategorySlug, query.SortBy, It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmptyPagedResult);

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _postRepositoryMock.Verify(
                x => x.GetByCategorySlugAsync(query.Paging, "technology", query.SortBy, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
