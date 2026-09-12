namespace blog.Domain.Users.Common
{
    public class UserActivityReport
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