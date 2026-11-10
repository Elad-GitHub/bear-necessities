using BearNecessities.Application.DTOs.Events;

namespace BearNecessities.Application.UseCases.Events.SearchEvents;

public record SearchEventsRequest(string? Query, int Limit = 6);

public record SearchEventsResponse(IReadOnlyList<EventSummaryDto> Items);
