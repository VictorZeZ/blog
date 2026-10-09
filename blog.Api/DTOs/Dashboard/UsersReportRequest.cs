using blog.Domain.Common.Enum;

namespace blog.Api.DTOs.Dashboard
{
    public class UsersReportRequest : ReportDateRangeRequest
    {
        public TriStateFilter EmailConfirmed { get; init; } = TriStateFilter.All;
        public TriStateFilter TwoFactorEnabled { get; init; } = TriStateFilter.All;
    }
}