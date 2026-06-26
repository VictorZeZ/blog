namespace blog.Domain.Common.Helpers
{
    public static class EmailNormalizer
    {
        public static string Normalize(string email)
        {
            var parts = email.ToLowerInvariant().Split('@');
            if (parts.Length != 2) return email.ToLowerInvariant();

            var local = parts[0];
            var domain = parts[1];

            if (domain is "gmail.com" or "googlemail.com")
                local = local.Replace(".", "");

            return $"{local}@{domain}";
        }
    }
}
