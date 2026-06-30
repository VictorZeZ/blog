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
            var folderName = folder switch
            {
                StorageFolder.Posts => "posts",
                StorageFolder.Avatars => "avatars",
                _ => throw new UnsupportedOperationException($"StorageFolder.{folder}")
            };

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                Folder = folderName,
                UseFilename = true,
                UniqueFilename = true,
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

        private static string ExtractPublicId(string url)
        {
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Split('/');

            var folderAndFile = string.Join('/', segments.Skip(5)); // posts/filename.jpg
            return Path.ChangeExtension(folderAndFile, null); // posts/filename
        }
    }
}
