using BearNecessities.Application.DTOs.Events;

namespace BearNecessities.Application.UseCases.Events.GetPendingEvents;

public record GetPendingEventsResponse(IReadOnlyList<EventSummaryDto> Items, Guid? NextCursor);
