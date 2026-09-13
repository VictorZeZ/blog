using blog.Application.Posts.Queries.GetPostsReport;
using blog.Domain.Categories.Entities;
using blog.Domain.Categories.Repository;
using blog.Domain.Common;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Entities;
using blog.Domain.Posts.Enums;
using blog.Domain.Posts.Repository;
using blog.Domain.Users.Entities;
using blog.Domain.Users.Repository;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Posts.Queries
{
    public class GetPostsReportQueryHandlerTests
    {
        private readonly Mock<IPostRepository> _postRepositoryMock = new();
        private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly GetPostsReportQueryHandler _handler;

        public GetPostsReportQueryHandlerTests()
        {
            _handler = new GetPostsReportQueryHandler(
                _postRepositoryMock.Object,
                _categoryRepositoryMock.Object,
                _userRepositoryMock.Object);
        }

        private static GetPostsReportQuery CreateQuery(
            DateOnly? from = null,
            DateOnly? to = null,
            PostFilter filter = PostFilter.All,
            PostSortBy sortBy = PostSortBy.Newest,
            Guid? categoryId = null,
            Guid? authorId = null)
        {
            return new GetPostsReportQuery
            {
                ActorId = Guid.NewGuid(),
                Paging = new PagedRequest
                {
                    Page = 1,
                    PageSize = 10
                },
                From = from,
                To = to,
                Filter = filter,
                SortBy = sortBy,
                CategoryId = categoryId,
                AuthorId = authorId
            };
        }

        private static PagedResult<Post> EmptyResult =>
            new([], 0, 1, 10);

        [Fact]
        public async Task Handle_ValidQuery_ReturnsPagedResult()
        {
            var from = new DateOnly(2026, 8, 1);
            var to = new DateOnly(2026, 8, 30);

            var author = new User(
                "author@test.com",
                "Ali",
                "Rezaei",
                "hashed_password");

            var category = new Category("Technology");

            var posts = new List<Post>
            {
                new(
                    "EF Core Guide",
                    "EF Core summary",
                    null,
                    "Content",
                    ["dotnet"],
                    author,
                    category)
            };

            var query = CreateQuery(
                from,
                to,
                PostFilter.All,
                PostSortBy.Newest);

            var pagedResult = new PagedResult<Post>(
                posts,
                1,
                1,
                10);

            _postRepositoryMock
                .Setup(x => x.GetReportAsync(
                    query.Paging,
                    from,
                    to,
                    query.Filter,
                    query.SortBy,
                    null,
                    null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(pagedResult);

            var result = await _handler.Handle(
                query,
                CancellationToken.None);

            result.Should().NotBeNull();
            result.TotalCount.Should().Be(1);
            result.Page.Should().Be(1);
            result.PageSize.Should().Be(10);
            result.Items.Should().HaveCount(1);

            result.Items.First().Title
                .Should().Be("EF Core Guide");

            result.Items.First().Summary
                .Should().Be("EF Core summary");
        }

        [Fact]
        public async Task Handle_ValidQuery_PassesParametersToRepository()
        {
            var from = new DateOnly(2026, 8, 1);
            var to = new DateOnly(2026, 8, 30);

            var query = CreateQuery(
                from,
                to,
                PostFilter.Published,
                PostSortBy.Oldest);

            _postRepositoryMock
                .Setup(x => x.GetReportAsync(
                    query.Paging,
                    from,
                    to,
                    PostFilter.Published,
                    PostSortBy.Oldest,
                    null,
                    null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmptyResult);

            await _handler.Handle(
                query,
                CancellationToken.None);

            _postRepositoryMock.Verify(
                x => x.GetReportAsync(
                    query.Paging,
                    from,
                    to,
                    PostFilter.Published,
                    PostSortBy.Oldest,
                    null,
                    null,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_QueryWithCategory_PassesCategoryToRepository()
        {
            var from = new DateOnly(2026, 8, 1);
            var to = new DateOnly(2026, 8, 30);
            var categoryId = Guid.NewGuid();

            var category = new Category("Technology");

            _categoryRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    It.IsAny<blog.Domain.Categories.Types.CategoryId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(category);

            var query = CreateQuery(
                from,
                to,
                categoryId: categoryId);

            _postRepositoryMock
                .Setup(x => x.GetReportAsync(
                    query.Paging,
                    from,
                    to,
                    query.Filter,
                    query.SortBy,
                    It.Is<blog.Domain.Categories.Types.CategoryId?>(
                        x => x!.Value.Value == categoryId),
                    null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmptyResult);

            await _handler.Handle(
                query,
                CancellationToken.None);

            _postRepositoryMock.Verify(
                x => x.GetReportAsync(
                    query.Paging,
                    from,
                    to,
                    query.Filter,
                    query.SortBy,
                    It.Is<blog.Domain.Categories.Types.CategoryId?>(
                        x => x!.Value.Value == categoryId),
                    null,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_CategoryDoesNotExist_ThrowsNotFoundException()
        {
            var categoryId = Guid.NewGuid();

            _categoryRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    It.IsAny<blog.Domain.Categories.Types.CategoryId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Category?)null);

            var query = CreateQuery(
                categoryId: categoryId);

            var act = () => _handler.Handle(
                query,
                CancellationToken.None);

            await act.Should()
                .ThrowAsync<NotFoundException>();

            _postRepositoryMock.Verify(
                x => x.GetReportAsync(
                    It.IsAny<PagedRequest>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<PostFilter>(),
                    It.IsAny<PostSortBy>(),
                    It.IsAny<blog.Domain.Categories.Types.CategoryId?>(),
                    It.IsAny<blog.Domain.Users.Types.UserId?>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_QueryWithAuthor_PassesAuthorToRepository()
        {
            var from = new DateOnly(2026, 8, 1);
            var to = new DateOnly(2026, 8, 30);
            var authorId = Guid.NewGuid();

            var author = new User(
                "author@test.com",
                "Ali",
                "Rezaei",
                "hashed_password");

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    It.IsAny<blog.Domain.Users.Types.UserId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(author);

            var query = CreateQuery(
                from,
                to,
                authorId: authorId);

            _postRepositoryMock
                .Setup(x => x.GetReportAsync(
                    query.Paging,
                    from,
                    to,
                    query.Filter,
                    query.SortBy,
                    null,
                    It.Is<blog.Domain.Users.Types.UserId?>(
                        x => x!.Value.Value == authorId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmptyResult);

            await _handler.Handle(
                query,
                CancellationToken.None);

            _postRepositoryMock.Verify(
                x => x.GetReportAsync(
                    query.Paging,
                    from,
                    to,
                    query.Filter,
                    query.SortBy,
                    null,
                    It.Is<blog.Domain.Users.Types.UserId?>(
                        x => x!.Value.Value == authorId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_AuthorDoesNotExist_ThrowsNotFoundException()
        {
            var authorId = Guid.NewGuid();

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    It.IsAny<blog.Domain.Users.Types.UserId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            var query = CreateQuery(
                authorId: authorId);

            var act = () => _handler.Handle(
                query,
                CancellationToken.None);

            await act.Should()
                .ThrowAsync<NotFoundException>();

            _postRepositoryMock.Verify(
                x => x.GetReportAsync(
                    It.IsAny<PagedRequest>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<PostFilter>(),
                    It.IsAny<PostSortBy>(),
                    It.IsAny<blog.Domain.Categories.Types.CategoryId?>(),
                    It.IsAny<blog.Domain.Users.Types.UserId?>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_RepositoryReturnsEmptyResult_ReturnsEmptyResult()
        {
            var from = new DateOnly(2026, 8, 1);
            var to = new DateOnly(2026, 8, 30);

            var query = CreateQuery(from, to);

            _postRepositoryMock
                .Setup(x => x.GetReportAsync(
                    query.Paging,
                    from,
                    to,
                    query.Filter,
                    query.SortBy,
                    null,
                    null,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(EmptyResult);

            var result = await _handler.Handle(
                query,
                CancellationToken.None);

            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }
    }
}