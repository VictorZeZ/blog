using blog.Application.Posts.Queries.GetAllPosts;
using blog.Domain.Categories.Types;
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
    public class GetAllPostsQueryHandlerTests
    {
        private readonly Mock<IPostRepository> _postRepositoryMock = new();
        private readonly GetAllPostsQueryHandler _handler;

        public GetAllPostsQueryHandlerTests()
        {
            _handler = new GetAllPostsQueryHandler(_postRepositoryMock.Object);
        }

        private static User CreateUser(string email, UserLevel level)
        {
            var user = new User(email, "Ali", "Rezaei", "hashed_password");
            if (level != UserLevel.Normal)
                user.Promote(level);

            return user;
        }

        private static GetAllPostsQuery QueryFor(PostFilter filter = PostFilter.All) => new()
        {
            ActorId = Guid.NewGuid(),
            Paging = new PagedRequest { Page = 1, PageSize = 10 },
            SortBy = PostSortBy.Newest,
            Filter = filter
        };

        private static PagedResult<Post> EmptyPagedResult => new([], 0, 1, 10);

        [Fact]
        public async Task Handle_ValidQuery_ReturnsPagedResult()
        {
            // Arrange
            var query = QueryFor();

            _postRepositoryMock
                .Setup(x => x.GetAllAsync(query.Paging, query.SortBy, query.Filter, It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmptyPagedResult);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_FilterByRejected_PassesFilterToRepository()
        {
            // Arrange
            var query = QueryFor(PostFilter.Rejected);

            _postRepositoryMock
                .Setup(x => x.GetAllAsync(query.Paging, query.SortBy, PostFilter.Rejected, It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmptyPagedResult);

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _postRepositoryMock.Verify(
                x => x.GetAllAsync(query.Paging, query.SortBy, PostFilter.Rejected, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidQuery_ReturnsCorrectPostData()
        {
            // Arrange
            var query = QueryFor();
            var author = CreateUser("author@test.com", UserLevel.Author);
            var posts = new List<Post> { new("Rejected Post", null, "Content", ["dotnet"], author, CategoryId.New()) };
            var pagedResult = new PagedResult<Post>(posts, 1, 1, 10);

            _postRepositoryMock
                .Setup(x => x.GetAllAsync(query.Paging, query.SortBy, query.Filter, It.IsAny<CancellationToken>()))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items.First().Title.Should().Be("Rejected Post");
        }
    }
}
