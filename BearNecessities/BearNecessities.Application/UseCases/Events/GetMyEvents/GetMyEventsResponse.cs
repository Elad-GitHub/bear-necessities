using BearNecessities.Application.DTOs.Events;

namespace BearNecessities.Application.UseCases.Events.GetMyEvents;

public record GetMyEventsResponse(IReadOnlyList<EventSummaryDto> Items);
