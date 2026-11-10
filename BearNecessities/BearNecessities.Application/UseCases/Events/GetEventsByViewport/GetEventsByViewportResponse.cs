using BearNecessities.Application.DTOs.Events;

namespace BearNecessities.Application.UseCases.Events.GetEventsByViewport;

public record GetEventsByViewportResponse(
    List<EventSummaryDto> Items,
    int TotalCount,
    Guid? NextCursor);
