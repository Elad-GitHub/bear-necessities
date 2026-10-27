using Nomadica.Application.DTOs.Events;

namespace Nomadica.Application.UseCases.Events.SearchEvents;

public record SearchEventsRequest(string? Query, int Limit = 6);

public record SearchEventsResponse(IReadOnlyList<EventSummaryDto> Items);
