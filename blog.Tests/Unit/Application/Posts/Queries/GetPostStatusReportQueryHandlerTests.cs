using blog.Application.Posts.Queries.GetPostStatusReport;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Repository;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Posts.Queries
{
    public class GetPostStatusReportQueryHandlerTests
    {
        private readonly Mock<IPostRepository> _postRepositoryMock = new();
        private readonly GetPostStatusReportQueryHandler _handler;

        public GetPostStatusReportQueryHandlerTests()
        {
            _handler = new GetPostStatusReportQueryHandler(
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
                DraftCount = 5,
                PendingApprovalCount = 3,
                PublishedCount = 20,
                RejectedCount = 2,
                TotalCount = 30
            };
        }

        private static GetPostStatusReportQuery CreateQuery(
            DateOnly from,
            DateOnly to)
        {
            return new GetPostStatusReportQuery
            {
                ActorId = Guid.NewGuid(),
                From = from,
                To = to
            };
        }

        [Fact]
        public async Task Handle_ValidQuery_ReturnsStatusReport()
        {
            // Arrange
            var from = new DateOnly(2026, 8, 1);
            var to = new DateOnly(2026, 8, 30);

            var query = CreateQuery(from, to);
            var report = CreateReport(from, to);

            _postRepositoryMock
                .Setup(x => x.GetStatusReportAsync(
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
        public async Task Handle_ValidQuery_PassesDateRangeToRepository()
        {
            // Arrange
            var from = new DateOnly(2026, 8, 1);
            var to = new DateOnly(2026, 8, 30);

            var query = CreateQuery(from, to);

            _postRepositoryMock
                .Setup(x => x.GetStatusReportAsync(
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
                x => x.GetStatusReportAsync(
                    from,
                    to,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_RepositoryReturnsZeroCounts_ReturnsZeroCounts()
        {
            // Arrange
            var from = new DateOnly(2026, 8, 1);
            var to = new DateOnly(2026, 8, 30);

            var query = CreateQuery(from, to);

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
                .Setup(x => x.GetStatusReportAsync(
                    from,
                    to,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(report);

            // Act
            var result = await _handler.Handle(
                query,
                CancellationToken.None);

            // Assert
            result.TotalCount.Should().Be(0);
            result.DraftCount.Should().Be(0);
            result.PendingApprovalCount.Should().Be(0);
            result.PublishedCount.Should().Be(0);
            result.RejectedCount.Should().Be(0);
        }
    }
}