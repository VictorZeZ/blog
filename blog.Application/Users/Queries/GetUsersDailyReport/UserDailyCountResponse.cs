namespace blog.Application.Users.Queries.GetUsersDailyReport
{
    public class UserDailyCountResponse
    {
        public DateOnly Date { get; init; }
        public int NormalCount { get; init; }
        public int AuthorCount { get; init; }
        public int AdminCount { get; init; }
        public int OwnerCount { get; init; }
        public int RegisteredCount { get; init; }
        public int BannedCount { get; init; }
        public int DeletedCount { get; init; }
    }
}