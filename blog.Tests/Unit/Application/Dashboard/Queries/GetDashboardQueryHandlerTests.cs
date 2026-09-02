using blog.Application.Dashboard.Queries.GetDashboard;
using blog.Domain.Categories.Repository;
using blog.Domain.Common;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Repository;
using blog.Domain.Users.Common;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Enums;
using blog.Domain.Users.Repository;
using blog.Domain.Users.Types;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Dashboard.Queries
{
    public class GetDashboardQueryHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IPostRepository> _postRepositoryMock = new();
        private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();

        private readonly GetDashboardQueryHandler _handler;

        public GetDashboardQueryHandlerTests()
        {
            _handler = new GetDashboardQueryHandler(
                _userRepositoryMock.Object,
                _postRepositoryMock.Object,
                _categoryRepositoryMock.Object);
        }

        private static User CreateUser(
            string email = "user@test.com",
            UserLevel level = UserLevel.Normal)
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

        private static PostStats CreatePostStats()
        {
            return new PostStats
            {
                TotalCount = 12,
                DraftCount = 3,
                PendingApprovalCount = 2,
                PublishedCount = 6,
                RejectedCount = 1,
                TotalViewCount = 1500,
                PostsPerDay =
                [
                    new DailyCount(
                        new DateOnly(2026, 9, 1),
                        2),
                    new DailyCount(
                        new DateOnly(2026, 9, 2),
                        1)
                ]
            };
        }

        private static UserStats CreateUserStats()
        {
            return new UserStats
            {
                TotalCount = 100,
                NormalCount = 70,
                AuthorCount = 20,
                AdminCount = 8,
                OwnerCount = 2,
                BannedCount = 4,
                RegistrationsPerDay =
                [
                    new DailyCount(
                        new DateOnly(2026, 9, 1),
                        5),
                    new DailyCount(
                        new DateOnly(2026, 9, 2),
                        3)
                ]
            };
        }

        private static GetDashboardQuery CreateQuery(Guid actorId)
        {
            return new GetDashboardQuery
            {
                ActorId = actorId
            };
        }

        private void SetupUser(User user)
        {
            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    user.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
        }

        private void SetupPostStats(User user)
        {
            _postRepositoryMock
                .Setup(x => x.GetStatsByAuthorAsync(
                    user.Id,
                    30,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreatePostStats());
        }

        [Fact]
        public async Task Handle_NormalUser_ReturnsProfileAndMyContent()
        {
            // Arrange
            var user = CreateUser();
            var query = CreateQuery(user.Id.Value);

            SetupUser(user);
            SetupPostStats(user);

            // Act
            var result = await _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            result.Should().NotBeNull();

            result.Profile.Should().NotBeNull();
            result.Profile.Id.Should().Be(user.Id.Value);
            result.Profile.FullName.Should().Be(user.FullName);
            result.Profile.Level.Should().Be(UserLevel.Normal);
            result.Profile.IsEmailConfirmed.Should().Be(user.IsEmailConfirmed);
            result.Profile.TwoFactorEnabled.Should().Be(user.TwoFactorEnabled);
            result.Profile.CreatedAt.Should().Be(user.CreatedAt);

            result.MyContent.Should().NotBeNull();
            result.MyContent.DraftCount.Should().Be(3);
            result.MyContent.PendingApprovalCount.Should().Be(2);
            result.MyContent.PublishedCount.Should().Be(6);
            result.MyContent.RejectedCount.Should().Be(1);
            result.MyContent.TotalViewCount.Should().Be(1500);

            result.AuthorInsights.Should().BeNull();
            result.ModerationQueue.Should().BeNull();
            result.PlatformStats.Should().BeNull();
            result.OwnerOverview.Should().BeNull();
        }

        [Fact]
        public async Task Handle_Author_ReturnsAuthorInsights()
        {
            // Arrange
            var user = CreateUser(
                "author@test.com",
                UserLevel.Author);

            var query = CreateQuery(user.Id.Value);

            var postStats = CreatePostStats();

            SetupUser(user);

            _postRepositoryMock
                .Setup(x => x.GetStatsByAuthorAsync(
                    user.Id,
                    30,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(postStats);

            // Act
            var result = await _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            result.AuthorInsights.Should().NotBeNull();

            result.AuthorInsights!.PostsPerDay
                .Should()
                .BeEquivalentTo(postStats.PostsPerDay);

            result.ModerationQueue.Should().BeNull();
            result.PlatformStats.Should().BeNull();
            result.OwnerOverview.Should().BeNull();

            _postRepositoryMock.Verify(
                x => x.GetStatsAsync(
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _userRepositoryMock.Verify(
                x => x.GetStatsAsync(
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _categoryRepositoryMock.Verify(
                x => x.GetActiveCountAsync(
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_Admin_ReturnsPlatformStatisticsAndModerationQueue()
        {
            // Arrange
            var admin = CreateUser(
                "admin@test.com",
                UserLevel.Admin);

            var query = CreateQuery(admin.Id.Value);

            var myPostStats = CreatePostStats();

            var platformPostStats = new PostStats
            {
                TotalCount = 500,
                DraftCount = 40,
                PendingApprovalCount = 15,
                PublishedCount = 420,
                RejectedCount = 25,
                TotalViewCount = 25000,
                PostsPerDay = []
            };

            var userStats = CreateUserStats();
            var activeCategoryCount = 12;

            SetupUser(admin);

            _postRepositoryMock
                .Setup(x => x.GetStatsByAuthorAsync(
                    admin.Id,
                    30,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(myPostStats);

            _postRepositoryMock
                .Setup(x => x.GetStatsAsync(
                    30,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(platformPostStats);

            _userRepositoryMock
                .Setup(x => x.GetStatsAsync(
                    30,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(userStats);

            _categoryRepositoryMock
                .Setup(x => x.GetActiveCountAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(activeCategoryCount);

            // Act
            var result = await _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            result.Should().NotBeNull();

            result.AuthorInsights.Should().NotBeNull();

            result.ModerationQueue.Should().NotBeNull();
            result.ModerationQueue!.PendingApprovalCount
                .Should()
                .Be(platformPostStats.PendingApprovalCount);

            result.ModerationQueue.ActiveCategoryCount
                .Should()
                .Be(activeCategoryCount);

            result.PlatformStats.Should().NotBeNull();
            result.PlatformStats!.TotalUserCount
                .Should()
                .Be(userStats.TotalCount);

            result.PlatformStats.BannedUserCount
                .Should()
                .Be(userStats.BannedCount);

            result.PlatformStats.TotalPostCount
                .Should()
                .Be(platformPostStats.TotalCount);

            result.PlatformStats.TotalViewCount
                .Should()
                .Be(platformPostStats.TotalViewCount);

            result.PlatformStats.RegistrationsPerDay
                .Should()
                .BeEquivalentTo(userStats.RegistrationsPerDay);

            result.OwnerOverview.Should().BeNull();
        }

        [Fact]
        public async Task Handle_Owner_ReturnsOwnerOverview()
        {
            // Arrange
            var owner = CreateUser(
                "owner@test.com",
                UserLevel.Owner);

            var query = CreateQuery(owner.Id.Value);

            SetupUser(owner);

            _postRepositoryMock
                .Setup(x => x.GetStatsByAuthorAsync(
                    owner.Id,
                    30,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreatePostStats());

            _postRepositoryMock
                .Setup(x => x.GetStatsAsync(
                    30,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PostStats
                {
                    TotalCount = 500,
                    PendingApprovalCount = 15
                });

            var userStats = CreateUserStats();

            _userRepositoryMock
                .Setup(x => x.GetStatsAsync(
                    30,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(userStats);

            _categoryRepositoryMock
                .Setup(x => x.GetActiveCountAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(12);

            // Act
            var result = await _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            result.OwnerOverview.Should().NotBeNull();

            result.OwnerOverview!.NormalCount
                .Should()
                .Be(userStats.NormalCount);

            result.OwnerOverview.AuthorCount
                .Should()
                .Be(userStats.AuthorCount);

            result.OwnerOverview.AdminCount
                .Should()
                .Be(userStats.AdminCount);

            result.OwnerOverview.OwnerCount
                .Should()
                .Be(userStats.OwnerCount);
        }

        [Fact]
        public async Task Handle_UserNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var query = CreateQuery(actorId);

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
        public async Task Handle_DeletedUser_ThrowsInvalidStateException()
        {
            // Arrange
            var user = CreateUser();
            user.SoftDelete();

            var query = CreateQuery(user.Id.Value);

            SetupUser(user);

            // Act
            var act = () => _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();

            _postRepositoryMock.Verify(
                x => x.GetStatsByAuthorAsync(
                    It.IsAny<UserId>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_BannedUser_ThrowsInvalidStateException()
        {
            // Arrange
            var user = CreateUser();
            user.Ban();

            var query = CreateQuery(user.Id.Value);

            SetupUser(user);

            // Act
            var act = () => _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidStateException>();

            _postRepositoryMock.Verify(
                x => x.GetStatsByAuthorAsync(
                    It.IsAny<UserId>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_NormalUser_DoesNotLoadPlatformStatistics()
        {
            // Arrange
            var user = CreateUser();
            var query = CreateQuery(user.Id.Value);

            SetupUser(user);
            SetupPostStats(user);

            // Act
            await _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            _postRepositoryMock.Verify(
                x => x.GetStatsAsync(
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _userRepositoryMock.Verify(
                x => x.GetStatsAsync(
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _categoryRepositoryMock.Verify(
                x => x.GetActiveCountAsync(
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_Admin_DoesNotReturnOwnerOverview()
        {
            // Arrange
            var admin = CreateUser(
                "admin@test.com",
                UserLevel.Admin);

            var query = CreateQuery(admin.Id.Value);

            SetupUser(admin);
            SetupPostStats(admin);

            _postRepositoryMock
                .Setup(x => x.GetStatsAsync(
                    30,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreatePostStats());

            _userRepositoryMock
                .Setup(x => x.GetStatsAsync(
                    30,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateUserStats());

            _categoryRepositoryMock
                .Setup(x => x.GetActiveCountAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(10);

            // Act
            var result = await _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            result.OwnerOverview.Should().BeNull();
        }
    }
}