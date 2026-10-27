using Nomadica.Domain.Enums;

namespace Nomadica.Application.DTOs.Places;

public record PlaceDetailDto(
    Guid Id,
    string Name,
    decimal Latitude,
    decimal Longitude,
    string H3IndexRes8,
    string? City,
    string? Country,
    ListingStatus Status,
    decimal? AverageRating,
    int ReviewCount,
    List<Guid> CategoryIds,
    string? ThumbnailUrl,
    bool IsPermanent,
    string? Description,
    string? Address,
    string Timezone,
    ListingSourceType Source,
    bool IsVerified,
    List<ExternalLinkDto> ExternalLinks,
    List<CategoryDto> Categories,
    List<MediaDto> Media,
    DateTime CreatedAt);
