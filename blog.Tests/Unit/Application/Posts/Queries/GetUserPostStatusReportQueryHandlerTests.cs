using blog.Application.Posts.Queries.GetUserPostStatusReport;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Repository;
using blog.Domain.Users.Types;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Posts.Queries
{
    public class GetUserPostStatusReportQueryHandlerTests
    {
        private readonly Mock<IPostRepository> _postRepositoryMock = new();
        private readonly GetUserPostStatusReportQueryHandler _handler;

        public GetUserPostStatusReportQueryHandlerTests()
        {
            _handler = new GetUserPostStatusReportQueryHandler(
                _postRepositoryMock.Object);
        }

        private static PostStatusReport CreateReport(
            DateOnly from,
            DateOnly to)
        {
            return new PostStatusReport
            {
                From = from,
                To = to,
                DraftCount = 4,
                PendingApprovalCount = 2,
                PublishedCount = 10,
                RejectedCount = 1,
                TotalCount = 17
            };
        }

        private static GetUserPostStatusReportQuery CreateQuery(
            Guid actorId,
            Guid authorId,
            DateOnly from,
            DateOnly to)
        {
            return new GetUserPostStatusReportQuery
            {
                ActorId = actorId,
                AuthorId = authorId,
                From = from,
                To = to
            };
        }

        [Fact]
        public async Task Handle_ValidQuery_ReturnsAuthorStatusReport()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var authorId = Guid.NewGuid();

            var from = new DateOnly(2026, 8, 1);
            var to = new DateOnly(2026, 8, 30);

            var query = CreateQuery(
                actorId,
                authorId,
                from,
                to);

            var report = CreateReport(from, to);

            _postRepositoryMock
                .Setup(x => x.GetStatusReportByAuthorAsync(
                    new UserId(authorId),
                    from,
                    to,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(report);

            // Act
            var result = await _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.AuthorId.Should().Be(authorId);
            result.From.Should().Be(from);
            result.To.Should().Be(to);
            result.DraftCount.Should().Be(report.DraftCount);
            result.PendingApprovalCount
                .Should()
                .Be(report.PendingApprovalCount);
            result.PublishedCount
                .Should()
                .Be(report.PublishedCount);
            result.RejectedCount
                .Should()
                .Be(report.RejectedCount);
            result.TotalCount.Should().Be(report.TotalCount);
        }

        [Fact]
        public async Task Handle_ValidQuery_PassesAuthorAndDateRangeToRepository()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var authorId = Guid.NewGuid();

            var from = new DateOnly(2026, 8, 1);
            var to = new DateOnly(2026, 8, 30);

            var query = CreateQuery(
                actorId,
                authorId,
                from,
                to);

            _postRepositoryMock
                .Setup(x => x.GetStatusReportByAuthorAsync(
                    new UserId(authorId),
                    from,
                    to,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateReport(from, to));

            // Act
            await _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            _postRepositoryMock.Verify(
                x => x.GetStatusReportByAuthorAsync(
                    new UserId(authorId),
                    from,
                    to,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_RepositoryReturnsZeroCounts_ReturnsZeroCounts()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var authorId = Guid.NewGuid();

            var from = new DateOnly(2026, 8, 1);
            var to = new DateOnly(2026, 8, 30);

            var query = CreateQuery(
                actorId,
                authorId,
                from,
                to);

            var report = new PostStatusReport
            {
                From = from,
                To = to,
                DraftCount = 0,
                PendingApprovalCount = 0,
                PublishedCount = 0,
                RejectedCount = 0,
                TotalCount = 0
            };

            _postRepositoryMock
                .Setup(x => x.GetStatusReportByAuthorAsync(
                    new UserId(authorId),
                    from,
                    to,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(report);

            // Act
            var result = await _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            result.AuthorId.Should().Be(authorId);
            result.TotalCount.Should().Be(0);
            result.DraftCount.Should().Be(0);
            result.PendingApprovalCount.Should().Be(0);
            result.PublishedCount.Should().Be(0);
            result.RejectedCount.Should().Be(0);
        }
    }
}