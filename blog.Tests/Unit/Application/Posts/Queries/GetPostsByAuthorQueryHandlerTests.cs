using blog.Application.Posts.Queries.GetPostsByAuthor;
using blog.Domain.Categories.Entities;
using blog.Domain.Common;
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
    public class GetPostsByAuthorQueryHandlerTests
    {
        private readonly Mock<IPostRepository> _postRepositoryMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly GetPostsByAuthorQueryHandler _handler;

        public GetPostsByAuthorQueryHandlerTests()
        {
            _handler = new GetPostsByAuthorQueryHandler(
                _postRepositoryMock.Object,
                _userRepositoryMock.Object);
        }

        private static User CreateUser(string email, UserLevel level)
        {
            var user = new User(email, "Ali", "Rezaei", "hashed_password");
            if (level != UserLevel.Normal)
                user.Promote(level);

            return user;
        }

        private static GetPostsByAuthorQuery QueryFor(Guid authorId, Guid? actorId = null) => new()
        {
            AuthorId = authorId,
            ActorId = actorId,
            Paging = new PagedRequest { Page = 1, PageSize = 10 },
            SortBy = PostSortBy.Newest
        };

        private static PagedResult<Post> EmptyPagedResult => new([], 0, 1, 10);

        [Fact]
        public async Task Handle_AnonymousActor_QueriesPublishedOnly()
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Author);
            var query = QueryFor(author.Id.Value, actorId: null);

            _postRepositoryMock
                .Setup(x => x.GetByAuthorAsync(query.Paging, author.Id, query.SortBy, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmptyPagedResult);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            _postRepositoryMock.Verify(
                x => x.GetByAuthorAsync(query.Paging, author.Id, query.SortBy, true, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ActorNotFound_QueriesPublishedOnly()
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Author);
            var query = QueryFor(author.Id.Value, actorId: Guid.NewGuid());

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(query.ActorId!.Value), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            _postRepositoryMock
                .Setup(x => x.GetByAuthorAsync(query.Paging, author.Id, query.SortBy, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmptyPagedResult);

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _postRepositoryMock.Verify(
                x => x.GetByAuthorAsync(query.Paging, author.Id, query.SortBy, true, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_UnrelatedNormalActor_QueriesPublishedOnly()
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Author);
            var otherActor = CreateUser("other@test.com", UserLevel.Normal);
            var query = QueryFor(author.Id.Value, otherActor.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(otherActor.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(otherActor);

            _postRepositoryMock
                .Setup(x => x.GetByAuthorAsync(query.Paging, author.Id, query.SortBy, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmptyPagedResult);

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _postRepositoryMock.Verify(
                x => x.GetByAuthorAsync(query.Paging, author.Id, query.SortBy, true, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_OwnerActor_QueriesAllStatuses()
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Author);
            var query = QueryFor(author.Id.Value, author.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(author.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(author);

            _postRepositoryMock
                .Setup(x => x.GetByAuthorAsync(query.Paging, author.Id, query.SortBy, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmptyPagedResult);

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _postRepositoryMock.Verify(
                x => x.GetByAuthorAsync(query.Paging, author.Id, query.SortBy, false, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory]
        [InlineData(UserLevel.Admin)]
        [InlineData(UserLevel.Owner)]
        public async Task Handle_ElevatedNonOwnerActor_QueriesAllStatuses(UserLevel actorLevel)
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Author);
            var elevatedActor = CreateUser("moderator@test.com", actorLevel);
            var query = QueryFor(author.Id.Value, elevatedActor.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(elevatedActor.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(elevatedActor);

            _postRepositoryMock
                .Setup(x => x.GetByAuthorAsync(query.Paging, author.Id, query.SortBy, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmptyPagedResult);

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _postRepositoryMock.Verify(
                x => x.GetByAuthorAsync(query.Paging, author.Id, query.SortBy, false, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidQuery_ReturnsCorrectPostData()
        {
            // Arrange
            var author = CreateUser("author@test.com", UserLevel.Admin);
            var query = QueryFor(author.Id.Value);
            var posts = new List<Post> { new("My Post", null, "Content", ["dotnet"], author, new Category("Technology")) };
            var pagedResult = new PagedResult<Post>(posts, 1, 1, 10);

            _postRepositoryMock
                .Setup(x => x.GetByAuthorAsync(query.Paging, author.Id, query.SortBy, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items.First().AuthorId.Should().Be(author.Id.Value);
        }
    }
}
