namespace blog.Domain.Posts.Common
{
    public sealed record UserOverviewCounts(int NormalCount, int AuthorCount, int AdminCount, int OwnerCount, int ActiveCount, int BannedCount, int DeletedCount, int EmailConfirmedCount, int TwoFactorEnabledCount);
}