using blog.Domain.Common.Enum;
using blog.Domain.Common.Interfaces;
using blog.Domain.Common.Settings;
using blog.Domain.Exceptions;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace blog.Infrastructure.Services
{
    public class CloudinaryService(IOptions<CloudinarySettings> settings) : IFileStorageService
    {
        private readonly Cloudinary _cloudinary = new(new Account(settings.Value.CloudName, settings.Value.ApiKey, settings.Value.ApiSecret));

        public async Task<string> UploadAsync(Stream fileStream, string fileName, StorageFolder folder, CancellationToken ct = default)
        {
            var folderName = ResolveFolderName(folder);
            var publicId = GenerateRandomPublicId();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                Folder = folderName,
                PublicId = publicId,
                UseFilename = false,
                UniqueFilename = false,
                Overwrite = false
            };

            var result = await _cloudinary.UploadAsync(uploadParams, ct);

            if (result.Error is not null)
                throw new UnavailableException("Cloudinary");

            return result.SecureUrl.ToString();
        }

        public async Task DeleteAsync(string imageUrl, CancellationToken ct = default)
        {
            var publicId = ExtractPublicId(imageUrl);
            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);

            if (result.Error is not null)
                throw new UnavailableException("Cloudinary");
        }

        private static string ResolveFolderName(StorageFolder folder) => folder switch
        {
            StorageFolder.Posts => "posts",
            StorageFolder.Avatars => "avatars",
            _ => throw new UnsupportedOperationException($"StorageFolder.{folder}")
        };

        // Fully random, unrelated to the original file name — GUID-based, no collisions, no leakage of the source name.
        private static string GenerateRandomPublicId()
            => Guid.CreateVersion7().ToString("N");

        private static string ExtractPublicId(string url)
        {
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Split('/');

            // Cloudinary delivery URL shape: /<cloud_name>/image/upload/[v<version>/]<folder>/<public_id>.<ext>
            var uploadIndex = Array.IndexOf(segments, "upload");
            if (uploadIndex < 0 || uploadIndex + 1 >= segments.Length)
                throw new UnsupportedOperationException("ExtractPublicId: unrecognized Cloudinary URL format");

            var afterUpload = segments.Skip(uploadIndex + 1);

            // Skip the optional version segment (e.g. "v1720000000")
            afterUpload = afterUpload.First().StartsWith('v') && afterUpload.First()[1..].All(char.IsDigit)
                ? afterUpload.Skip(1)
                : afterUpload;

            var folderAndFile = string.Join('/', afterUpload);
            return Path.ChangeExtension(folderAndFile, null);
        }
    }
}
