using blog.Domain.Common.Enum;

namespace blog.Domain.Common.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> UploadAsync(Stream fileStream, string fileName, StorageFolder folder, CancellationToken ct = default);
        Task DeleteAsync(string imageUrl, CancellationToken ct = default);
    }
}
