using Nomadica.Application.DTOs.Events;

namespace Nomadica.Application.UseCases.Events.GetMyEvents;

public record GetMyEventsResponse(IReadOnlyList<EventSummaryDto> Items);
