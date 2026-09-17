using blog.Application.Posts.Queries.GetAllPosts;
using blog.Domain.Categories.Entities;
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
        private readonly Mock<IPostRepository> _postRepositoryMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly GetAllPostsQueryHandler _handler;

        public GetAllPostsQueryHandlerTests()
        {
            _handler = new GetAllPostsQueryHandler(_postRepositoryMock.Object, _userRepositoryMock.Object);
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

        private void SetupActor(Guid actorId, UserLevel level)
        {
            var actor = CreateUser("actor@test.com", level);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(new UserId(actorId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(actor);
        }

        [Fact]
        public async Task Handle_ValidQuery_ReturnsPagedResult()
        {
            // Arrange
            var query = QueryFor();
            SetupActor(query.ActorId, UserLevel.Admin);

            _postRepositoryMock
                .Setup(x => x.GetAllAsync(query.Paging, query.SortBy, query.Filter, false, It.IsAny<CancellationToken>()))
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
            SetupActor(query.ActorId, UserLevel.Admin);

            _postRepositoryMock
                .Setup(x => x.GetAllAsync(query.Paging, query.SortBy, PostFilter.Rejected, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmptyPagedResult);

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _postRepositoryMock.Verify(
                x => x.GetAllAsync(query.Paging, query.SortBy, PostFilter.Rejected, false, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidQuery_ReturnsCorrectPostData()
        {
            // Arrange
            var query = QueryFor();
            SetupActor(query.ActorId, UserLevel.Admin);

            var author = CreateUser("author@test.com", UserLevel.Author);
            var posts = new List<Post> { new("Rejected Post", "Summary for post", null, "Content", ["dotnet"], author, new Category("Technology")) };
            var pagedResult = new PagedResult<Post>(posts, 1, 1, 10);

            _postRepositoryMock
                .Setup(x => x.GetAllAsync(query.Paging, query.SortBy, query.Filter, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items.First().Title.Should().Be("Rejected Post");
        }

        [Fact]
        public async Task Handle_OwnerActor_IncludesDrafts()
        {
            // Arrange
            var query = QueryFor();
            SetupActor(query.ActorId, UserLevel.Owner);

            _postRepositoryMock
                .Setup(x => x.GetAllAsync(query.Paging, query.SortBy, query.Filter, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmptyPagedResult);

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _postRepositoryMock.Verify(
                x => x.GetAllAsync(query.Paging, query.SortBy, query.Filter, true, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_AdminActor_FilterDraft_ThrowsForbiddenException()
        {
            // Arrange
            var query = QueryFor(PostFilter.Draft);
            SetupActor(query.ActorId, UserLevel.Admin);

            // Act
            var act = () => _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();

            _postRepositoryMock.Verify(
                x => x.GetAllAsync(It.IsAny<PagedRequest>(), It.IsAny<PostSortBy>(), It.IsAny<PostFilter>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_OwnerActor_FilterDraft_ReturnsResult()
        {
            // Arrange
            var query = QueryFor(PostFilter.Draft);
            SetupActor(query.ActorId, UserLevel.Owner);

            _postRepositoryMock
                .Setup(x => x.GetAllAsync(query.Paging, query.SortBy, PostFilter.Draft, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmptyPagedResult);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
        }
    }
}