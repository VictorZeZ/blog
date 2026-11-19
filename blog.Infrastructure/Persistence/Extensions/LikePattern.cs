namespace blog.Infrastructure.Persistence.Extensions
{
    public static class LikePattern
    {
        public static string Contains(string term)
            => $"%{Escape(term)}%";

        private static string Escape(string term)
            => term
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");
    }
}