namespace Nomadica.Application.DTOs.Media;

public record PresignedUploadResult(
    string UploadUrl,
    string ObjectKey,
    string CdnUrl,
    DateTime ExpiresAt);
