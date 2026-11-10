using BearNecessities.Application.DTOs;
using BearNecessities.Domain.Enums;

namespace BearNecessities.Application.DTOs.Events;

public record EventDetailDto(
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
    string? Description,
    string? RRule,
    List<DateTime> ExDates,
    string? OnlineUrl,
    int? MaxCapacity,
    string Timezone,
    DateTime CreatedAt,
    List<CategoryDto> Categories,
    List<MediaDto> Media,
    List<OccurrenceDto> UpcomingOccurrences);
