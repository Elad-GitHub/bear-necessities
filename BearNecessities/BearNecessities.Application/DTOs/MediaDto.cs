using BearNecessities.Domain.Enums;

namespace BearNecessities.Application.DTOs;

public record MediaDto(
    Guid Id,
    string Url,
    string? ThumbnailUrl,
    string MimeType,
    int SortOrder,
    ModerationStatus ModerationStatus,
    string? UploadedByName = null,
    Guid? ReviewId = null);
