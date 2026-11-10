using BearNecessities.Domain.Enums;

namespace BearNecessities.Application.DTOs.Places;

public record PlaceSummaryDto(
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
    string? RejectionReason = null,
    string? Description = null,
    string? Address = null);
