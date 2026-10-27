namespace Nomadica.Application.UseCases.Media.RequestUpload;

public record RequestUploadResponse(
    Guid MediaId,
    string UploadUrl,
    string ObjectKey,
    string CdnUrl,
    DateTime ExpiresAt);
