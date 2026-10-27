using Nomadica.Application.DTOs.Events;

namespace Nomadica.Application.UseCases.Events.GetPendingEvents;

public record GetPendingEventsResponse(IReadOnlyList<EventSummaryDto> Items, Guid? NextCursor);
