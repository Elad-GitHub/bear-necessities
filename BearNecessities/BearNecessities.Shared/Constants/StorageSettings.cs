namespace BearNecessities.Shared.Constants;

public record StorageSettings
{
    public string Provider { get; init; } = "R2";
    public string BucketName { get; init; } = string.Empty;
    public string AccountId { get; init; } = string.Empty;
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string CdnBaseUrl { get; init; } = string.Empty;
    public int PresignedUrlExpiryMinutes { get; init; } = 15;
    public long MaxFileSizeBytes { get; init; } = 10_485_760;
    public List<string> AllowedMimeTypes { get; init; } = [];
}
