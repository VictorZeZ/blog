using blog.Domain.Common.Interfaces;

namespace blog.Domain.Common.Helpers
{
    public static class UnitOfWorkExtensions
    {
        public static async Task SaveChangesOrDiscardUploadAsync(this IUnitOfWork unitOfWork, IFileStorageService fileStorageService, string? uploadedImageUrl, CancellationToken ct = default)
        {
            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch
            {
                if (uploadedImageUrl is not null)
                    await fileStorageService.TryDeleteAsync(uploadedImageUrl);

                throw;
            }
        }
    }
}