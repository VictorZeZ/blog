using blog.Application.Posts.Queries.GetCategoryPostStatusReport;
using blog.Domain.Categories.Entities;
using blog.Domain.Categories.Repository;
using blog.Domain.Exceptions;
using blog.Domain.Posts.Common;
using blog.Domain.Posts.Repository;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Posts.Queries
{
    public class GetCategoryPostStatusReportQueryHandlerTests
    {
        private readonly Mock<IPostRepository> _postRepositoryMock = new();
        private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();
        private readonly GetCategoryPostStatusReportQueryHandler _handler;

        public GetCategoryPostStatusReportQueryHandlerTests()
        {
            _handler = new GetCategoryPostStatusReportQueryHandler(
                _postRepositoryMock.Object,
                _categoryRepositoryMock.Object);
        }

        private static GetCategoryPostStatusReportQuery CreateQuery(
            Guid categoryId,
            DateOnly? from = null,
            DateOnly? to = null)
        {
            return new GetCategoryPostStatusReportQuery
            {
                ActorId = Guid.NewGuid(),
                CategoryId = categoryId,
                From = from,
                To = to
            };
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

        [Fact]
        public async Task Handle_ValidQuery_ReturnsCategoryStatusReport()
        {
            var categoryId = Guid.NewGuid();
            var from = new DateOnly(2026, 8, 1);
            var to = new DateOnly(2026, 8, 30);

            var query = CreateQuery(categoryId, from, to);
            var category = new Category("Technology");
            var report = CreateReport(from, to);

            _categoryRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    It.IsAny<blog.Domain.Categories.Types.CategoryId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(category);

            _postRepositoryMock
                .Setup(x => x.GetStatusReportByCategoryAsync(
                    It.IsAny<blog.Domain.Categories.Types.CategoryId>(),
                    from,
                    to,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(report);

            var result = await _handler.Handle(
                query,
                CancellationToken.None);

            result.Should().NotBeNull();
            result.CategoryId.Should().Be(categoryId);
            result.From.Should().Be(from);
            result.To.Should().Be(to);
            result.DraftCount.Should().Be(5);
            result.PendingApprovalCount.Should().Be(3);
            result.PublishedCount.Should().Be(20);
            result.RejectedCount.Should().Be(2);
            result.TotalCount.Should().Be(30);
        }

        [Fact]
        public async Task Handle_ValidQuery_PassesCategoryAndDateRangeToRepository()
        {
            var categoryId = Guid.NewGuid();
            var from = new DateOnly(2026, 8, 1);
            var to = new DateOnly(2026, 8, 30);

            var query = CreateQuery(categoryId, from, to);
            var category = new Category("Technology");

            _categoryRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    It.IsAny<blog.Domain.Categories.Types.CategoryId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(category);

            _postRepositoryMock
                .Setup(x => x.GetStatusReportByCategoryAsync(
                    It.Is<blog.Domain.Categories.Types.CategoryId>(
                        x => x.Value == categoryId),
                    from,
                    to,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateReport(from, to));

            await _handler.Handle(
                query,
                CancellationToken.None);

            _postRepositoryMock.Verify(
                x => x.GetStatusReportByCategoryAsync(
                    It.Is<blog.Domain.Categories.Types.CategoryId>(
                        id => id.Value == categoryId),
                    from,
                    to,
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

            var query = CreateQuery(categoryId);

            var act = () => _handler.Handle(
                query,
                CancellationToken.None);

            await act.Should()
                .ThrowAsync<NotFoundException>();

            _postRepositoryMock.Verify(
                x => x.GetStatusReportByCategoryAsync(
                    It.IsAny<blog.Domain.Categories.Types.CategoryId>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_QueryWithoutDateRange_UsesResolvedDateRange()
        {
            var categoryId = Guid.NewGuid();
            var category = new Category("Technology");
            var query = CreateQuery(categoryId);

            _categoryRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    It.IsAny<blog.Domain.Categories.Types.CategoryId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(category);

            _postRepositoryMock
                .Setup(x => x.GetStatusReportByCategoryAsync(
                    It.IsAny<blog.Domain.Categories.Types.CategoryId>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    (blog.Domain.Categories.Types.CategoryId categoryId,
                     DateOnly from,
                     DateOnly to,
                     CancellationToken _) => CreateReport(from, to));

            var result = await _handler.Handle(
                query,
                CancellationToken.None);

            result.Should().NotBeNull();

            _postRepositoryMock.Verify(
                x => x.GetStatusReportByCategoryAsync(
                    It.Is<blog.Domain.Categories.Types.CategoryId>(
                        id => id.Value == categoryId),
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_RepositoryReturnsZeroCounts_ReturnsZeroCounts()
        {
            var categoryId = Guid.NewGuid();
            var from = new DateOnly(2026, 8, 1);
            var to = new DateOnly(2026, 8, 30);

            var category = new Category("Technology");
            var query = CreateQuery(categoryId, from, to);

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

            _categoryRepositoryMock
                .Setup(x => x.GetByIdAsync(
                    It.IsAny<blog.Domain.Categories.Types.CategoryId>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(category);

            _postRepositoryMock
                .Setup(x => x.GetStatusReportByCategoryAsync(
                    It.IsAny<blog.Domain.Categories.Types.CategoryId>(),
                    from,
                    to,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(report);

            var result = await _handler.Handle(
                query,
                CancellationToken.None);

            result.DraftCount.Should().Be(0);
            result.PendingApprovalCount.Should().Be(0);
            result.PublishedCount.Should().Be(0);
            result.RejectedCount.Should().Be(0);
            result.TotalCount.Should().Be(0);
        }
    }
}