using Nomadica.Application.DTOs.Events;

namespace Nomadica.Application.UseCases.Events.GetEventsByViewport;

public record GetEventsByViewportResponse(
    List<EventSummaryDto> Items,
    int TotalCount,
    Guid? NextCursor);
