using blog.Domain.Exceptions;

namespace blog.Domain.Posts.Common
{
    public static class PostImageValidationRules
    {
        public const long MaxTitleImageSizeBytes = 5 * 1024 * 1024; // 5 MB
        private const int MagicByteReadLength = 12;

        public static readonly IReadOnlySet<string> AllowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/webp",
            "image/gif"
        };

        private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
        private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        private static readonly byte[] GifSignature = [0x47, 0x49, 0x46, 0x38];
        private static readonly byte[] RiffMarker = [0x52, 0x49, 0x46, 0x46];
        private static readonly byte[] WebpMarker = [0x57, 0x45, 0x42, 0x50];

        public static void EnsureValid(string fileName, long sizeBytes, string contentType)
        {
            if (sizeBytes > MaxTitleImageSizeBytes)
                throw new PayloadTooLargeException(fileName, MaxTitleImageSizeBytes);

            if (!AllowedContentTypes.Contains(contentType))
                throw new UnsupportedMediaTypeException(contentType, AllowedContentTypes);
        }

        public static async Task EnsureValidContentAsync(Stream fileStream, string contentType, CancellationToken ct = default)
        {
            if (!fileStream.CanSeek)
                throw new UnsupportedOperationException("Uploaded file stream must be seekable for content validation");

            var originalPosition = fileStream.Position;
            var header = new byte[MagicByteReadLength];
            var bytesRead = await fileStream.ReadAsync(header.AsMemory(0, header.Length), ct);
            fileStream.Position = originalPosition;

            if (!MatchesSignature(header, bytesRead, contentType))
                throw new UnsupportedMediaTypeException(contentType, AllowedContentTypes);
        }

        private static bool MatchesSignature(byte[] header, int bytesRead, string contentType)
        {
            if (contentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase))
                return bytesRead >= 12
                    && header[..4].SequenceEqual(RiffMarker)
                    && header[8..12].SequenceEqual(WebpMarker);

            if (contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
                return bytesRead >= PngSignature.Length && header[..PngSignature.Length].SequenceEqual(PngSignature);

            if (contentType.Equals("image/gif", StringComparison.OrdinalIgnoreCase))
                return bytesRead >= GifSignature.Length && header[..GifSignature.Length].SequenceEqual(GifSignature);

            if (contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase) || contentType.Equals("image/jpg", StringComparison.OrdinalIgnoreCase))
                return bytesRead >= JpegSignature.Length && header[..JpegSignature.Length].SequenceEqual(JpegSignature);

            return false;
        }
    }
}
