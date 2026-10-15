using System.Globalization;
using System.Text;

namespace blog.Domain.Common.Helpers
{
    public static class SlugGenerator
    {
        private const char Separator = '-';

        public static string Generate(string value)
        {
            var builder = new StringBuilder(value.Length);
            var hasPendingSeparator = false;

            foreach (var rune in value.ToLowerInvariant().EnumerateRunes())
            {
                if (!IsSlugCharacter(rune))
                {
                    hasPendingSeparator = true;
                    continue;
                }

                if (hasPendingSeparator && builder.Length > 0)
                    builder.Append(Separator);

                hasPendingSeparator = false;
                builder.Append(rune);
            }

            return builder.ToString();
        }

        private static bool IsSlugCharacter(Rune rune)
            => Rune.IsLetterOrDigit(rune)
               || Rune.GetUnicodeCategory(rune) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark;
    }
}