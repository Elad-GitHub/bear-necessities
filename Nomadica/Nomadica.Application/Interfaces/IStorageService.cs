using Nomadica.Application.DTOs.Media;

namespace Nomadica.Application.Interfaces;

public interface IStorageService
{
    Task<PresignedUploadResult> GeneratePresignedUploadUrlAsync(
        string objectKey,
        string mimeType,
        long maxSizeBytes,
        CancellationToken cancellationToken = default);

    Task DeleteObjectAsync(string objectKey, CancellationToken cancellationToken = default);
    Task<bool> ObjectExistsAsync(string objectKey, CancellationToken cancellationToken = default);

    /// Streams an object back through our own API, sidestepping the public bucket URL.
    Task<StorageDownload?> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default);
}
