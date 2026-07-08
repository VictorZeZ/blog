namespace blog.Domain.Common.Helpers
{
    public static class SlugGenerator
    {
        public static string Generate(string value)
        {
            return value
                .ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("_", "-");
        }
    }
}
