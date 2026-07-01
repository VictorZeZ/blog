using blog.Application.Posts.Queries.GetPendingApprovalPosts;
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
    public class GetPendingApprovalPostsQueryHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IPostRepository> _postRepositoryMock = new();
        private readonly GetPendingApprovalPostsQueryHandler _handler;

        public GetPendingApprovalPostsQueryHandlerTests()
        {
            _handler = new GetPendingApprovalPostsQueryHandler(
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

        private static GetPendingApprovalPostsQuery QueryFor(Guid actorId) => new()
        {
            ActorId = actorId,
            Paging = new PagedRequest { Page = 1, PageSize = 10 },
            SortBy = PostSortBy.Newest
        };

        private static PagedResult<Post> EmptyPagedResult => new([], 0, 1, 10);

        [Theory]
        [InlineData(UserLevel.Admin)]
        [InlineData(UserLevel.Owner)]
        public async Task Handle_ElevatedActor_ReturnsPagedResult(UserLevel level)
        {
            // Arrange
            var actor = CreateUser("mod@test.com", level);
            var query = QueryFor(actor.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(actor.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            _postRepositoryMock
                .Setup(x => x.GetPendingApprovalAsync(query.Paging, query.SortBy, It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmptyPagedResult);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
        }

        [Theory]
        [InlineData(UserLevel.Normal)]
        [InlineData(UserLevel.Author)]
        public async Task Handle_NonElevatedActor_ThrowsForbiddenException(UserLevel level)
        {
            // Arrange
            var actor = CreateUser("user@test.com", level);
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
            var actor = CreateUser("admin@test.com", UserLevel.Admin);
            actor.SoftDelete();
            var query = QueryFor(actor.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(actor.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            // Act
            var act = () => _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_BannedActor_ThrowsInvalidStateException()
        {
            // Arrange
            var actor = CreateUser("admin@test.com", UserLevel.Admin);
            actor.Ban();
            var query = QueryFor(actor.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(actor.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            // Act
            var act = () => _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();
        }

        [Fact]
        public async Task Handle_ValidQuery_ReturnsCorrectPostData()
        {
            // Arrange
            var actor = CreateUser("admin@test.com", UserLevel.Admin);
            var pendingAuthor = CreateUser("author@test.com", UserLevel.Author);
            var query = QueryFor(actor.Id.Value);
            var posts = new List<Post> { new("Pending Post", null, "Content", ["dotnet"], pendingAuthor) };
            var pagedResult = new PagedResult<Post>(posts, 1, 1, 10);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(actor.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            _postRepositoryMock
                .Setup(x => x.GetPendingApprovalAsync(query.Paging, query.SortBy, It.IsAny<CancellationToken>()))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items.First().Status.Should().Be(PostStatus.PendingApproval);
        }
    }
}
