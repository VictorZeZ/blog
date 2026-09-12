namespace blog.Application.Users.Queries.GetUserActivityReport
{
    public class GetUserActivityReportResponse
    {
        public DateOnly From { get; init; }
        public DateOnly To { get; init; }
        public int NewRegistrationsCount { get; init; }
        public int ConfirmedCount { get; init; }
        public int BannedCount { get; init; }
        public int DeletedCount { get; init; }
        public int NewNormalCount { get; init; }
        public int NewAuthorCount { get; init; }
        public int NewAdminCount { get; init; }
        public int NewOwnerCount { get; init; }
    }
}