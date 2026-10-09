namespace blog.Application.Users.Queries.GetUsersDailyReport
{
    public class UserLevelTotalsResponse
    {
        public int NormalCount { get; init; }
        public int AuthorCount { get; init; }
        public int AdminCount { get; init; }
        public int OwnerCount { get; init; }
    }
}