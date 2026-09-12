using blog.Domain.Common.Reports;
using blog.Domain.Users.Repository;
using MediatR;

namespace blog.Application.Users.Queries.GetUserActivityReport
{
    public class GetUserActivityReportQueryHandler(IUserRepository userRepository) : IRequestHandler<GetUserActivityReportQuery, GetUserActivityReportResponse>
    {
        public async Task<GetUserActivityReportResponse> Handle(GetUserActivityReportQuery request, CancellationToken cancellationToken)
        {
            var range = ReportDateRangeRules.Resolve(request.From, request.To);

            var report = await userRepository.GetActivityReportAsync(range.From, range.To, cancellationToken);

            return new GetUserActivityReportResponse
            {
                From = report.From,
                To = report.To,
                NewRegistrationsCount = report.NewRegistrationsCount,
                ConfirmedCount = report.ConfirmedCount,
                BannedCount = report.BannedCount,
                DeletedCount = report.DeletedCount,
                NewNormalCount = report.NewNormalCount,
                NewAuthorCount = report.NewAuthorCount,
                NewAdminCount = report.NewAdminCount,
                NewOwnerCount = report.NewOwnerCount
            };
        }
    }
}