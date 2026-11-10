namespace BearNecessities.Application.UseCases.Media.ConfirmUpload;

public record ConfirmUploadResponse(Guid MediaId, string CdnUrl, string ModerationStatus);
