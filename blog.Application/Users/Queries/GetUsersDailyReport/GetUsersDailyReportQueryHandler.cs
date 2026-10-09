using blog.Domain.Common.Reports;
using blog.Domain.Users.Repository;
using MediatR;

namespace blog.Application.Users.Queries.GetUsersDailyReport
{
    public class GetUsersDailyReportQueryHandler(IUserRepository userRepository) : IRequestHandler<GetUsersDailyReportQuery, UsersDailyReportResponse>
    {
        public async Task<UsersDailyReportResponse> Handle(GetUsersDailyReportQuery request, CancellationToken cancellationToken)
        {
            var range = ReportDateRangeRules.Resolve(request.From, request.To);

            var days = await userRepository.GetDailyActivityReportAsync(range.From, range.To, request.EmailConfirmed, request.TwoFactorEnabled, cancellationToken);

            var levelTotals = new UserLevelTotalsResponse
            {
                NormalCount = days.Sum(d => d.NormalCount),
                AuthorCount = days.Sum(d => d.AuthorCount),
                AdminCount = days.Sum(d => d.AdminCount),
                OwnerCount = days.Sum(d => d.OwnerCount)
            };

            return new UsersDailyReportResponse
            {
                From = range.From,
                To = range.To,
                EmailConfirmed = request.EmailConfirmed,
                TwoFactorEnabled = request.TwoFactorEnabled,
                RegisteredCount = levelTotals.NormalCount + levelTotals.AuthorCount + levelTotals.AdminCount + levelTotals.OwnerCount,
                BannedCount = days.Sum(d => d.BannedCount),
                DeletedCount = days.Sum(d => d.DeletedCount),
                LevelTotals = levelTotals,
                Days = days.Select(d => new UserDailyCountResponse
                {
                    Date = d.Date,
                    NormalCount = d.NormalCount,
                    AuthorCount = d.AuthorCount,
                    AdminCount = d.AdminCount,
                    OwnerCount = d.OwnerCount,
                    RegisteredCount = d.NormalCount + d.AuthorCount + d.AdminCount + d.OwnerCount,
                    BannedCount = d.BannedCount,
                    DeletedCount = d.DeletedCount
                }).ToList()
            };
        }
    }
}