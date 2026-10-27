using Nomadica.Application.DTOs.Media;
using Nomadica.Application.Interfaces;
using Nomadica.Shared.Constants;

namespace Nomadica.Infrastructure.Services;

public sealed class MockStorageService : IStorageService
{
    private readonly StorageSettings _settings;

    public MockStorageService(StorageSettings settings)
    {
        _settings = settings;
    }

    public Task<PresignedUploadResult> GeneratePresignedUploadUrlAsync(
        string objectKey,
        string mimeType,
        long maxSizeBytes,
        CancellationToken cancellationToken = default)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.PresignedUrlExpiryMinutes);
        var fakeUploadUrl = $"https://mock-storage.local/upload/{Uri.EscapeDataString(objectKey)}?mock=true&expires={expiresAt:O}";
        var cdnUrl = $"{_settings.CdnBaseUrl.TrimEnd('/')}/{objectKey}";

        return Task.FromResult(new PresignedUploadResult(fakeUploadUrl, objectKey, cdnUrl, expiresAt));
    }

    public Task DeleteObjectAsync(string objectKey, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<bool> ObjectExistsAsync(string objectKey, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<StorageDownload?> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default)
        => Task.FromResult<StorageDownload?>(null);
}
