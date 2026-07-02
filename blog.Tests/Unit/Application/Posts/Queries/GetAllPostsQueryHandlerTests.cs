using blog.Application.Posts.Queries.GetAllPosts;
using blog.Domain.Common;
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
    public class GetAllPostsQueryHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IPostRepository> _postRepositoryMock = new();
        private readonly GetAllPostsQueryHandler _handler;

        public GetAllPostsQueryHandlerTests()
        {
            _handler = new GetAllPostsQueryHandler(
                _userRepositoryMock.Object,
                _postRepositoryMock.Object);
        }

        private static User CreateUser(string email, UserLevel level)
        {
            var user = new User(email, "Ali", "Rezaei", "hashed_password");
            if (level != UserLevel.Normal)
                user.Promote(level);

            return user;
        }

        private static GetAllPostsQuery QueryFor(Guid actorId, PostFilter filter = PostFilter.All) => new()
        {
            ActorId = actorId,
            Paging = new PagedRequest { Page = 1, PageSize = 10 },
            SortBy = PostSortBy.Newest,
            Filter = filter
        };

        private static PagedResult<Post> EmptyPagedResult => new([], 0, 1, 10);

        [Fact]
        public async Task Handle_OwnerActor_ReturnsPagedResult()
        {
            // Arrange
            var owner = CreateUser("owner@test.com", UserLevel.Owner);
            var query = QueryFor(owner.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(owner.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(owner);

            _postRepositoryMock
                .Setup(x => x.GetAllAsync(query.Paging, query.SortBy, query.Filter, It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmptyPagedResult);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
        }

        [Theory]
        [InlineData(UserLevel.Normal)]
        [InlineData(UserLevel.Author)]
        [InlineData(UserLevel.Admin)]
        public async Task Handle_NonOwnerActor_ThrowsForbiddenException(UserLevel level)
        {
            // Arrange
            var actor = CreateUser("actor@test.com", level);
            var query = QueryFor(actor.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(actor.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            // Act
            var act = () => _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task Handle_ActorNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var query = QueryFor(Guid.NewGuid());

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(query.ActorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var act = () => _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Handle_DeletedActor_ThrowsInvalidStateException()
        {
            // Arrange
            var owner = CreateUser("owner@test.com", UserLevel.Owner);
            owner.SoftDelete();
            var query = QueryFor(owner.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(owner.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(owner);

            // Act
            var act = () => _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_BannedActor_ThrowsInvalidStateException()
        {
            // Arrange
            var owner = CreateUser("owner@test.com", UserLevel.Owner);
            owner.Ban();
            var query = QueryFor(owner.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(owner.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(owner);

            // Act
            var act = () => _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_FilterByRejected_PassesFilterToRepository()
        {
            // Arrange
            var owner = CreateUser("owner@test.com", UserLevel.Owner);
            var query = QueryFor(owner.Id.Value, PostFilter.Rejected);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(owner.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(owner);

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
            var owner = CreateUser("owner@test.com", UserLevel.Owner);
            var author = CreateUser("author@test.com", UserLevel.Author);
            var query = QueryFor(owner.Id.Value);
            var posts = new List<Post> { new("Rejected Post", null, "Content", ["dotnet"], author) };
            var pagedResult = new PagedResult<Post>(posts, 1, 1, 10);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(owner.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(owner);

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
