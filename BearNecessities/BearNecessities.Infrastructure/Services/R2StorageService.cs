using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using BearNecessities.Application.DTOs.Media;
using BearNecessities.Application.Interfaces;
using BearNecessities.Shared.Constants;
using System.Net;

namespace BearNecessities.Infrastructure.Services;

public sealed class R2StorageService : IStorageService, IDisposable
{
    private readonly AmazonS3Client _s3Client;
    private readonly StorageSettings _settings;
    private readonly ILogger<R2StorageService> _logger;

    public R2StorageService(StorageSettings settings, ILogger<R2StorageService> logger)
    {
        _settings = settings;
        _logger = logger;

        var credentials = new BasicAWSCredentials(settings.AccessKey, settings.SecretKey);
        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{settings.AccountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true
        };
        _s3Client = new AmazonS3Client(credentials, config);
    }

    public async Task<PresignedUploadResult> GeneratePresignedUploadUrlAsync(
        string objectKey,
        string mimeType,
        long maxSizeBytes,
        CancellationToken cancellationToken = default)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.PresignedUrlExpiryMinutes);

        var presignRequest = new GetPreSignedUrlRequest
        {
            BucketName = _settings.BucketName,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = expiresAt,
            ContentType = mimeType,
            Protocol = Protocol.HTTPS
        };

        var uploadUrl = await _s3Client.GetPreSignedURLAsync(presignRequest);
        var cdnUrl = $"{_settings.CdnBaseUrl.TrimEnd('/')}/{objectKey}";

        return new PresignedUploadResult(uploadUrl, objectKey, cdnUrl, expiresAt);
    }

    public async Task DeleteObjectAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        try
        {
            await _s3Client.DeleteObjectAsync(_settings.BucketName, objectKey, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Attempted to delete non-existent object {Key}", objectKey);
        }
    }

    public async Task<bool> ObjectExistsAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        try
        {
            await _s3Client.GetObjectMetadataAsync(_settings.BucketName, objectKey, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<StorageDownload?> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _s3Client.GetObjectAsync(_settings.BucketName, objectKey, cancellationToken);
            var contentType = string.IsNullOrWhiteSpace(response.Headers.ContentType)
                ? "application/octet-stream"
                : response.Headers.ContentType;
            return new StorageDownload(response.ResponseStream, contentType);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public void Dispose() => _s3Client.Dispose();
}
