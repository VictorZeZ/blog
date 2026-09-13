using blog.Application.Users.Queries.GetUserActivityReport;
using blog.Domain.Users.Common;
using blog.Domain.Users.Repository;
using FluentAssertions;
using Moq;

namespace blog.Tests.Unit.Application.Users.Queries
{
    public class GetUserActivityReportQueryHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly GetUserActivityReportQueryHandler _handler;

        public GetUserActivityReportQueryHandlerTests()
        {
            _handler = new GetUserActivityReportQueryHandler(
                _userRepositoryMock.Object);
        }

        private static GetUserActivityReportQuery CreateQuery(
            DateOnly? from = null,
            DateOnly? to = null)
        {
            return new GetUserActivityReportQuery
            {
                ActorId = Guid.NewGuid(),
                From = from,
                To = to
            };
        }

        private static UserActivityReport CreateReport(
            DateOnly from,
            DateOnly to)
        {
            return new UserActivityReport
            {
                From = from,
                To = to,
                NewRegistrationsCount = 25,
                ConfirmedCount = 20,
                BannedCount = 3,
                DeletedCount = 2,
                NewNormalCount = 15,
                NewAuthorCount = 7,
                NewAdminCount = 2,
                NewOwnerCount = 1
            };
        }

        [Fact]
        public async Task Handle_ValidQuery_ReturnsActivityReport()
        {
            var from = new DateOnly(2026, 8, 1);
            var to = new DateOnly(2026, 8, 30);

            var query = CreateQuery(from, to);
            var report = CreateReport(from, to);

            _userRepositoryMock
                .Setup(x => x.GetActivityReportAsync(
                    from,
                    to,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(report);

            var result = await _handler.Handle(
                query,
                CancellationToken.None);

            result.Should().NotBeNull();
            result.From.Should().Be(from);
            result.To.Should().Be(to);
            result.NewRegistrationsCount.Should().Be(25);
            result.ConfirmedCount.Should().Be(20);
            result.BannedCount.Should().Be(3);
            result.DeletedCount.Should().Be(2);
            result.NewNormalCount.Should().Be(15);
            result.NewAuthorCount.Should().Be(7);
            result.NewAdminCount.Should().Be(2);
            result.NewOwnerCount.Should().Be(1);
        }

        [Fact]
        public async Task Handle_ValidQuery_PassesDateRangeToRepository()
        {
            var from = new DateOnly(2026, 8, 1);
            var to = new DateOnly(2026, 8, 30);

            var query = CreateQuery(from, to);

            _userRepositoryMock
                .Setup(x => x.GetActivityReportAsync(
                    from,
                    to,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateReport(from, to));

            await _handler.Handle(
                query,
                CancellationToken.None);

            _userRepositoryMock.Verify(
                x => x.GetActivityReportAsync(
                    from,
                    to,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_QueryWithoutDateRange_UsesResolvedDateRange()
        {
            var query = CreateQuery();

            _userRepositoryMock
                .Setup(x => x.GetActivityReportAsync(
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    (DateOnly from, DateOnly to, CancellationToken _) =>
                        CreateReport(from, to));

            var result = await _handler.Handle(
                query,
                CancellationToken.None);

            result.Should().NotBeNull();

            _userRepositoryMock.Verify(
                x => x.GetActivityReportAsync(
                    It.IsAny<DateOnly>(),
                    It.IsAny<DateOnly>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_RepositoryReturnsZeroCounts_ReturnsZeroCounts()
        {
            var from = new DateOnly(2026, 8, 1);
            var to = new DateOnly(2026, 8, 30);

            var query = CreateQuery(from, to);

            var report = new UserActivityReport
            {
                From = from,
                To = to,
                NewRegistrationsCount = 0,
                ConfirmedCount = 0,
                BannedCount = 0,
                DeletedCount = 0,
                NewNormalCount = 0,
                NewAuthorCount = 0,
                NewAdminCount = 0,
                NewOwnerCount = 0
            };

            _userRepositoryMock
                .Setup(x => x.GetActivityReportAsync(
                    from,
                    to,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(report);

            var result = await _handler.Handle(
                query,
                CancellationToken.None);

            result.NewRegistrationsCount.Should().Be(0);
            result.ConfirmedCount.Should().Be(0);
            result.BannedCount.Should().Be(0);
            result.DeletedCount.Should().Be(0);
            result.NewNormalCount.Should().Be(0);
            result.NewAuthorCount.Should().Be(0);
            result.NewAdminCount.Should().Be(0);
            result.NewOwnerCount.Should().Be(0);
        }
    }
}