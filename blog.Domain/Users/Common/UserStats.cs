using blog.Domain.Common;

namespace blog.Domain.Users.Common
{
    public class UserStats
    {
        public int TotalCount { get; init; }
        public int NormalCount { get; init; }
        public int AuthorCount { get; init; }
        public int AdminCount { get; init; }
        public int OwnerCount { get; init; }
        public int BannedCount { get; init; }
        public IReadOnlyList<DailyCount> RegistrationsPerDay { get; init; } = [];
    }
}