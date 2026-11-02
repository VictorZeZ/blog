using AngleSharp.Dom;
using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using Ganss.Xss;
using System.Text.RegularExpressions;

namespace blog.Infrastructure.Services
{
    public partial class HtmlContentSanitizer : IContentSanitizer
    {
        private const string LinkRel = "noopener noreferrer nofollow ugc";

        private static readonly string[] PermittedTags =
        [
            "p", "br", "hr", "h1", "h2", "h3", "h4", "h5", "h6",
            "strong", "b", "em", "i", "u", "s", "strike", "del", "ins", "sub", "sup", "mark",
            "code", "pre", "blockquote", "ul", "ol", "li", "a", "img",
            "table", "thead", "tbody", "tr", "th", "td"
        ];

        private static readonly string[] PermittedSchemes = ["http", "https", "mailto"];

        private static readonly string[] PermittedAttributes =
        [
            "href", "title", "target", "src", "alt", "width", "height", "colspan", "rowspan", "style", "class"
        ];

        private static readonly string[] PermittedCssProperties = ["text-align", "color", "background-color"];

        private readonly HtmlSanitizer _sanitizer = CreateSanitizer();

        public string Sanitize(string html)
        {
            var sanitized = _sanitizer.Sanitize(html);
            if (string.IsNullOrWhiteSpace(sanitized))
                throw new ValidationException("Content", "Content contains no valid HTML");

            return sanitized;
        }

        private static HtmlSanitizer CreateSanitizer()
        {
            var sanitizer = new HtmlSanitizer();

            Replace(sanitizer.AllowedTags, PermittedTags);
            Replace(sanitizer.AllowedSchemes, PermittedSchemes);
            Replace(sanitizer.AllowedAttributes, PermittedAttributes);
            Replace(sanitizer.AllowedCssProperties, PermittedCssProperties);

            sanitizer.PostProcessNode += (_, e) =>
            {
                if (e.Node is IElement element)
                    RestrictElement(element);
            };

            return sanitizer;
        }

        private static void Replace(ISet<string> target, IEnumerable<string> values)
        {
            target.Clear();
            target.UnionWith(values);
        }

        private static void RestrictElement(IElement element)
        {
            switch (element.LocalName)
            {
                case "a":
                    element.SetAttribute("rel", LinkRel);
                    if (element.GetAttribute("target") is { } target && target != "_blank")
                        element.RemoveAttribute("target");
                    break;

                case "img":
                    if (element.GetAttribute("src") is not { } src || !src.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        element.RemoveAttribute("src");
                    break;
            }

            RestrictClasses(element);
        }

        private static void RestrictClasses(IElement element)
        {
            var value = element.GetAttribute("class");
            if (value is null)
                return;

            var allowed = element.LocalName == "code"
                ? value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(c => CodeLanguageClass().IsMatch(c)).ToArray()
                : [];

            if (allowed.Length == 0)
                element.RemoveAttribute("class");
            else
                element.SetAttribute("class", string.Join(' ', allowed));
        }

        [GeneratedRegex("^language-[a-z0-9_+#-]{1,30}$", RegexOptions.IgnoreCase)]
        private static partial Regex CodeLanguageClass();
    }
}