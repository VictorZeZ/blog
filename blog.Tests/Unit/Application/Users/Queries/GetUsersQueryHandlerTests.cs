using blog.Application.Users.Queries.GetUsers;
using blog.Domain.Common;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Enums;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Users.Queries
{
    public class GetUsersQueryHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly GetUsersQueryHandler _handler;

        public GetUsersQueryHandlerTests()
        {
            _handler = new GetUsersQueryHandler(_userRepositoryMock.Object);
        }

        private static User CreateUser(string email, UserLevel level)
        {
            var user = new User(email, "Ali", "Rezaei", "hashed_password");
            if (level != UserLevel.Normal)
                user.Promote(level);
            return user;
        }

        private static GetUsersQuery ValidQuery(Guid actorId) => new()
        {
            ActorId = actorId,
            Paging = new PagedRequest { Page = 1, PageSize = 10 },
            SortBy = UserSortBy.Newest,
            Filter = UserFilter.All
        };

        private static PagedResult<User> EmptyPagedResult => new([], 0, 1, 10);

        [Fact]
        public async Task Handle_AdminActor_ReturnsPagedResult()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var actor = CreateUser("admin@test.com", UserLevel.Admin);
            var query = ValidQuery(actorId);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(actorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            _userRepositoryMock
                .Setup(x => x.GetAllAsync(query.Paging, query.SortBy, query.Filter, It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmptyPagedResult);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_OwnerActor_ReturnsPagedResult()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var actor = CreateUser("owner@test.com", UserLevel.Owner);
            var query = ValidQuery(actorId);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(actorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);

            _userRepositoryMock
                .Setup(x => x.GetAllAsync(query.Paging, query.SortBy, query.Filter, It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmptyPagedResult);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_ValidQuery_ReturnsPagedResult()
        {
            // Arrange
            var query = ValidQuery(Guid.NewGuid());

            _userRepositoryMock
                .Setup(x => x.GetAllAsync(query.Paging, query.SortBy, query.Filter, It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmptyPagedResult);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_ValidQuery_ReturnsCorrectUserData()
        {
            // Arrange
            var query = ValidQuery(Guid.NewGuid());

            var users = new List<User>
            {
                CreateUser("user1@test.com", UserLevel.Normal),
                CreateUser("user2@test.com", UserLevel.Author)
            };

            var pagedResult = new PagedResult<User>(users, 2, 1, 10);

            _userRepositoryMock
                .Setup(x => x.GetAllAsync(query.Paging, query.SortBy, query.Filter, It.IsAny<CancellationToken>()))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.TotalCount.Should().Be(2);
            result.Items.Should().HaveCount(2);
            result.Items.First().Email.Should().Be("user1@test.com");
            result.Items.Last().Level.Should().Be(UserLevel.Author);
        }
    }
}
