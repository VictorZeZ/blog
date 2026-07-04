using blog.Domain.Exceptions;

namespace blog.Domain.Posts.Common
{
    public static class PostImageValidationRules
    {
        public const long MaxTitleImageSizeBytes = 5 * 1024 * 1024; // 5 MB

        public static readonly IReadOnlySet<string> AllowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/webp",
            "image/gif"
        };

        public static void EnsureValid(string fileName, long sizeBytes, string contentType)
        {
            if (sizeBytes > MaxTitleImageSizeBytes)
                throw new PayloadTooLargeException(fileName, MaxTitleImageSizeBytes);

            if (!AllowedContentTypes.Contains(contentType))
                throw new UnsupportedMediaTypeException(contentType, AllowedContentTypes);
        }
    }
}
