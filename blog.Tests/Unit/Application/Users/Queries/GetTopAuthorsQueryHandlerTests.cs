using blog.Application.Users.Queries.GetTopAuthors;
using blog.Domain.Posts.Repository;
using blog.Domain.Users.Common;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Users.Queries
{
    public class GetTopAuthorsQueryHandlerTests
    {
        private readonly Mock<IPostRepository> _postRepositoryMock = new();
        private readonly GetTopAuthorsQueryHandler _handler;

        public GetTopAuthorsQueryHandlerTests()
        {
            _handler = new GetTopAuthorsQueryHandler(
                _postRepositoryMock.Object);
        }

        private static GetTopAuthorsQuery CreateQuery(
            DateOnly? from = null,
            DateOnly? to = null,
            int topN = 5)
        {
            return new GetTopAuthorsQuery
            {
                ActorId = Guid.NewGuid(),
                From = from,
                To = to,
                TopN = topN
            };
        }

        [Fact]
        public async Task Handle_ValidQuery_ReturnsTopAuthors()
        {
            // Arrange
            var from = new DateOnly(2026, 8, 1);
            var to = new DateOnly(2026, 8, 30);

            var query = CreateQuery(from, to, 5);

            var authors = new List<TopAuthorResult>
            {
                new()
                {
                    AuthorId = Guid.NewGuid(),
                    FullName = "Ali Rezaei",
                    PublishedCount = 25
                },
                new()
                {
                    AuthorId = Guid.NewGuid(),
                    FullName = "Sara Ahmadi",
                    PublishedCount = 18
                }
            };

            _postRepositoryMock
                .Setup(x => x.GetTopAuthorsAsync(
                    from,
                    to,
                    5,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(authors);

            // Act
            var result = await _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);

            result[0].AuthorId.Should().Be(authors[0].AuthorId);
            result[0].FullName.Should().Be("Ali Rezaei");
            result[0].PublishedCount.Should().Be(25);

            result[1].AuthorId.Should().Be(authors[1].AuthorId);
            result[1].FullName.Should().Be("Sara Ahmadi");
            result[1].PublishedCount.Should().Be(18);
        }

        [Fact]
        public async Task Handle_ValidQuery_PassesDateRangeAndTopNToRepository()
        {
            // Arrange
            var from = new DateOnly(2026, 8, 1);
            var to = new DateOnly(2026, 8, 30);

            var query = CreateQuery(from, to, 10);

            _postRepositoryMock
                .Setup(x => x.GetTopAuthorsAsync(
                    from,
                    to,
                    10,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            await _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            _postRepositoryMock.Verify(
                x => x.GetTopAuthorsAsync(
                    from,
                    to,
                    10,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_QueryWithoutDateRange_UsesResolvedDateRange()
        {
            // Arrange
            var query = CreateQuery(
                from: null,
                to: null,
                topN: 5);

            _postRepositoryMock
                .Setup(x => x.GetTopAuthorsAsync(
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    5,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            await _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            _postRepositoryMock.Verify(
                x => x.GetTopAuthorsAsync(
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    5,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_RepositoryReturnsEmptyList_ReturnsEmptyResult()
        {
            // Arrange
            var from = new DateOnly(2026, 8, 1);
            var to = new DateOnly(2026, 8, 30);

            var query = CreateQuery(from, to, 5);

            _postRepositoryMock
                .Setup(x => x.GetTopAuthorsAsync(
                    from,
                    to,
                    5,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            // Act
            var result = await _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
    }
}