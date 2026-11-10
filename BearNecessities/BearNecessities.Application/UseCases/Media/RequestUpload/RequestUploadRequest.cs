using BearNecessities.Domain.Enums;

namespace BearNecessities.Application.UseCases.Media.RequestUpload;

public record RequestUploadRequest(
    MediaOwnerType OwnerType,
    Guid OwnerId,
    string FileName,
    string MimeType,
    long FileSizeBytes);
