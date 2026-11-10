using BearNecessities.Domain.Enums;

namespace BearNecessities.Application.DTOs.Events;

public record EventSummaryDto(
    Guid Id,
    string Title,
    Guid? PlaceId,
    decimal? Latitude,
    decimal? Longitude,
    DateTime? StartAt,
    DateTime? EndAt,
    EventType EventType,
    bool IsFree,
    decimal? Price,
    string? Currency,
    ListingStatus Status,
    List<Guid> CategoryIds,
    string? ThumbnailUrl,
    decimal? AverageRating,
    string? RejectionReason = null,
    string? Description = null);
