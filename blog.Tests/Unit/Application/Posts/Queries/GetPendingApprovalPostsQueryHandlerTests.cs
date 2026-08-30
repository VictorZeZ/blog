using blog.Application.Posts.Queries.GetPendingApprovalPosts;
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
    public class GetPendingApprovalPostsQueryHandlerTests
    {
        private readonly Mock<IPostRepository> _postRepositoryMock = new();
        private readonly GetPendingApprovalPostsQueryHandler _handler;

        public GetPendingApprovalPostsQueryHandlerTests()
        {
            _handler = new GetPendingApprovalPostsQueryHandler(
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

        private static Post CreatePendingPost()
        {
            var author = CreateUser(UserLevel.Normal);

            return new Post(
                "Pending Post",
                "Summary for post",
                null,
                "Content",
                ["dotnet"],
                author,
                new Category("Technology"));
        }

        private static GetPendingApprovalPostsQuery ValidQuery => new()
        {
            ActorId = Guid.NewGuid(),
            Paging = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            },
            SortBy = PostSortBy.Newest
        };

        private static PagedResult<Post> EmptyPagedResult =>
            new([], 0, 1, 10);

        [Fact]
        public async Task Handle_ValidQuery_ReturnsPagedResult()
        {
            // Arrange
            var query = ValidQuery;

            _postRepositoryMock
                .Setup(x => x.GetPendingApprovalAsync(
                    query.Paging,
                    query.SortBy,
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
            var post = CreatePendingPost();

            var pagedResult = new PagedResult<Post>(
                [post],
                1,
                1,
                10);

            _postRepositoryMock
                .Setup(x => x.GetPendingApprovalAsync(
                    query.Paging,
                    query.SortBy,
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

            item.Title.Should().Be(post.Title);
            item.Summary.Should().Be(post.Summary);
            item.Status.Should().Be(PostStatus.PendingApproval);
            item.Tags.Should().Contain("dotnet");
        }
    }
}