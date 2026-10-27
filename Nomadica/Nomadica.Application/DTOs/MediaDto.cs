using Nomadica.Domain.Enums;

namespace Nomadica.Application.DTOs;

public record MediaDto(
    Guid Id,
    string Url,
    string? ThumbnailUrl,
    string MimeType,
    int SortOrder,
    ModerationStatus ModerationStatus);
