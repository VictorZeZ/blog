using blog.Application.Users.Queries.SearchUsers;
using blog.Domain.Common;
using blog.Domain.Users.Common;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Enums;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Users.Queries
{
    public class SearchUsersQueryHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly SearchUsersQueryHandler _handler;

        public SearchUsersQueryHandlerTests()
        {
            _handler = new SearchUsersQueryHandler(_userRepositoryMock.Object);
        }

        private static User CreateUser(UserLevel level)
        {
            var user = new User("test@test.com", "Ali", "Rezaei", "hashed_password");

            if (level != UserLevel.Normal)
                typeof(User).GetProperty(nameof(User.Level))!.SetValue(user, level);

            return user;
        }

        private static SearchUsersQuery CreateQuery(Guid? actorId, string term = "ali") => new()
        {
            Term = term,
            ActorId = actorId
        };

        private static PagedResult<UserSearchResult> CreateEmptyResult(SearchUsersQuery query) => new(
            [],
            0,
            query.Paging.Page,
            query.Paging.PageSize);

        // ── No actor (anonymous request) ────────────────────────────────

        [Fact]
        public async Task Handle_NoActorId_SearchesAsNonElevated()
        {
            // Arrange
            var query = CreateQuery(actorId: null);

            _userRepositoryMock
                .Setup(x => x.SearchAsync(query.Paging, query.Term, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateEmptyResult(query));

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _userRepositoryMock.Verify(x => x.SearchAsync(query.Paging, query.Term, false, It.IsAny<CancellationToken>()), Times.Once);
            _userRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<UserId>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ── Actor not found (e.g. stale/invalid token) ──────────────────

        [Fact]
        public async Task Handle_ActorNotFound_SearchesAsNonElevated()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var query = CreateQuery(actorId);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(actorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            _userRepositoryMock
                .Setup(x => x.SearchAsync(query.Paging, query.Term, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateEmptyResult(query));

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _userRepositoryMock.Verify(x => x.SearchAsync(query.Paging, query.Term, false, It.IsAny<CancellationToken>()), Times.Once);
        }

        // ── Actor is Normal ───────────────────────────────────────────────

        [Fact]
        public async Task Handle_ActorIsNormalLevel_SearchesAsNonElevated()
        {
            // Arrange
            var actor = CreateUser(UserLevel.Normal);
            var query = CreateQuery(actor.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(actor.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            _userRepositoryMock
                .Setup(x => x.SearchAsync(query.Paging, query.Term, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateEmptyResult(query));

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _userRepositoryMock.Verify(x => x.SearchAsync(query.Paging, query.Term, false, It.IsAny<CancellationToken>()), Times.Once);
        }

        // ── Actor is Author ───────────────────────────────────────────────

        [Fact]
        public async Task Handle_ActorIsAuthorLevel_SearchesAsNonElevated()
        {
            // Arrange
            var actor = CreateUser(UserLevel.Author);
            var query = CreateQuery(actor.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(actor.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            _userRepositoryMock
                .Setup(x => x.SearchAsync(query.Paging, query.Term, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateEmptyResult(query));

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _userRepositoryMock.Verify(x => x.SearchAsync(query.Paging, query.Term, false, It.IsAny<CancellationToken>()), Times.Once);
        }

        // ── Actor is Admin ────────────────────────────────────────────────

        [Fact]
        public async Task Handle_ActorIsAdminLevel_SearchesAsElevated()
        {
            // Arrange
            var actor = CreateUser(UserLevel.Admin);
            var query = CreateQuery(actor.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(actor.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            _userRepositoryMock
                .Setup(x => x.SearchAsync(query.Paging, query.Term, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateEmptyResult(query));

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _userRepositoryMock.Verify(x => x.SearchAsync(query.Paging, query.Term, true, It.IsAny<CancellationToken>()), Times.Once);
        }

        // ── Actor is Owner ────────────────────────────────────────────────

        [Fact]
        public async Task Handle_ActorIsOwnerLevel_SearchesAsElevated()
        {
            // Arrange
            var actor = CreateUser(UserLevel.Owner);
            var query = CreateQuery(actor.Id.Value);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(actor.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            _userRepositoryMock
                .Setup(x => x.SearchAsync(query.Paging, query.Term, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateEmptyResult(query));

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _userRepositoryMock.Verify(x => x.SearchAsync(query.Paging, query.Term, true, It.IsAny<CancellationToken>()), Times.Once);
        }

        // ── Pass-through of repository result ───────────────────────────

        [Fact]
        public async Task Handle_ReturnsRepositoryResultUnchanged()
        {
            // Arrange
            var query = CreateQuery(actorId: null);

            var expectedItem = new UserSearchResult
            {
                Id = Guid.NewGuid(),
                FullName = "Ali Rezaei",
                Email = null,
                Level = UserLevel.Author,
                CreatedAt = DateTime.UtcNow,
                PublishedPostsCount = 3,
                TotalViewCount = 42
            };

            var expectedResult = new PagedResult<UserSearchResult>([expectedItem], 1, query.Paging.Page, query.Paging.PageSize);

            _userRepositoryMock
                .Setup(x => x.SearchAsync(query.Paging, query.Term, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeSameAs(expectedResult);
            result.Items.Should().ContainSingle().Which.Should().Be(expectedItem);
        }
    }
}