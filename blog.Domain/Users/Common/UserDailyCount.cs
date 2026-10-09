namespace blog.Domain.Users.Common
{
    public sealed record UserDailyCount(DateOnly Date, int NormalCount, int AuthorCount, int AdminCount, int OwnerCount, int BannedCount, int DeletedCount);
}