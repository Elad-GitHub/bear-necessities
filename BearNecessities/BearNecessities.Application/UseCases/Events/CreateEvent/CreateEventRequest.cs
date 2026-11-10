using BearNecessities.Domain.Enums;

namespace BearNecessities.Application.UseCases.Events.CreateEvent;

public record CreateEventRequest(
    Guid? PlaceId,
    string Title,
    string? Description,
    EventType EventType,
    DateTime? StartAt,
    DateTime? EndAt,
    string Timezone,
    string? RRule,
    decimal? Price,
    string? Currency,
    bool IsFree,
    bool IsOnline,
    string? OnlineUrl,
    int? MaxCapacity,
    List<Guid> CategoryIds);
