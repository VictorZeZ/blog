using blog.Application.Categories.Queries.GetCategoryPerformanceReport;
using blog.Domain.Categories.Common;
using blog.Domain.Posts.Repository;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Categories.Queries
{
    public class GetCategoryPerformanceReportQueryHandlerTests
    {
        private readonly Mock<IPostRepository> _postRepositoryMock = new();
        private readonly GetCategoryPerformanceReportQueryHandler _handler;

        public GetCategoryPerformanceReportQueryHandlerTests()
        {
            _handler = new GetCategoryPerformanceReportQueryHandler(
                _postRepositoryMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnCategoryPerformanceReport()
        {
            // Arrange
            var from = new DateOnly(2026, 1, 1);
            var to = new DateOnly(2026, 1, 31);

            var categoryId = Guid.NewGuid();

            var breakdown = new List<CategoryPerformanceResult>
            {
                new()
                {
                    CategoryId = categoryId,
                    Name = "Technology",
                    DraftCount = 2,
                    PendingApprovalCount = 3,
                    PublishedCount = 10,
                    RejectedCount = 1,
                    TotalCount = 16,
                    TotalViewCount = 2500
                }
            };

            _postRepositoryMock
                .Setup(x => x.GetCategoryBreakdownAsync(
                    from,
                    to,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(breakdown);

            var query = new GetCategoryPerformanceReportQuery
            {
                From = from,
                To = to
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.From.Should().Be(from);
            result.To.Should().Be(to);

            result.Categories.Should().HaveCount(1);

            var category = result.Categories.Single();

            category.CategoryId.Should().Be(categoryId);
            category.Name.Should().Be("Technology");
            category.DraftCount.Should().Be(2);
            category.PendingApprovalCount.Should().Be(3);
            category.PublishedCount.Should().Be(10);
            category.RejectedCount.Should().Be(1);
            category.TotalCount.Should().Be(16);
            category.TotalViewCount.Should().Be(2500);
        }

        [Fact]
        public async Task Handle_ShouldPassDateRangeToRepository()
        {
            // Arrange
            var from = new DateOnly(2026, 2, 1);
            var to = new DateOnly(2026, 2, 28);

            _postRepositoryMock
                .Setup(x => x.GetCategoryBreakdownAsync(
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            var query = new GetCategoryPerformanceReportQuery
            {
                From = from,
                To = to
            };

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _postRepositoryMock.Verify(x =>
                x.GetCategoryBreakdownAsync(
                    from,
                    to,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldUseResolvedDateRange_WhenDatesAreNull()
        {
            // Arrange
            _postRepositoryMock
                .Setup(x => x.GetCategoryBreakdownAsync(
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            var query = new GetCategoryPerformanceReportQuery
            {
                From = null,
                To = null
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            var expectedRange = blog.Domain.Common.Reports.ReportDateRangeRules.Resolve(null, null);

            result.From.Should().Be(expectedRange.From);
            result.To.Should().Be(expectedRange.To);

            _postRepositoryMock.Verify(x =>
                x.GetCategoryBreakdownAsync(
                    expectedRange.From,
                    expectedRange.To,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnEmptyCategories_WhenRepositoryReturnsEmpty()
        {
            // Arrange
            var from = new DateOnly(2026, 3, 1);
            var to = new DateOnly(2026, 3, 31);

            _postRepositoryMock
                .Setup(x => x.GetCategoryBreakdownAsync(
                    from,
                    to,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            var query = new GetCategoryPerformanceReportQuery
            {
                From = from,
                To = to
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Categories.Should().BeEmpty();
            result.From.Should().Be(from);
            result.To.Should().Be(to);
        }

        [Fact]
        public async Task Handle_ShouldMapMultipleCategories()
        {
            // Arrange
            var from = new DateOnly(2026, 4, 1);
            var to = new DateOnly(2026, 4, 30);

            var firstCategoryId = Guid.NewGuid();
            var secondCategoryId = Guid.NewGuid();

            var breakdown = new List<CategoryPerformanceResult>
            {
                new()
                {
                    CategoryId = firstCategoryId,
                    Name = "Technology",
                    DraftCount = 1,
                    PendingApprovalCount = 2,
                    PublishedCount = 3,
                    RejectedCount = 4,
                    TotalCount = 10,
                    TotalViewCount = 100
                },
                new()
                {
                    CategoryId = secondCategoryId,
                    Name = "Programming",
                    DraftCount = 5,
                    PendingApprovalCount = 6,
                    PublishedCount = 7,
                    RejectedCount = 8,
                    TotalCount = 26,
                    TotalViewCount = 500
                }
            };

            _postRepositoryMock
                .Setup(x => x.GetCategoryBreakdownAsync(
                    from,
                    to,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(breakdown);

            var query = new GetCategoryPerformanceReportQuery
            {
                From = from,
                To = to
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Categories.Should().HaveCount(2);

            result.Categories[0].CategoryId.Should().Be(firstCategoryId);
            result.Categories[0].Name.Should().Be("Technology");

            result.Categories[1].CategoryId.Should().Be(secondCategoryId);
            result.Categories[1].Name.Should().Be("Programming");
        }
    }
}