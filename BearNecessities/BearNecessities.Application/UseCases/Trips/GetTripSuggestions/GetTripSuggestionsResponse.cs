using BearNecessities.Application.DTOs.Events;
using BearNecessities.Application.DTOs.Places;

namespace BearNecessities.Application.UseCases.Trips.GetTripSuggestions;

public record GetTripSuggestionsResponse(
    IReadOnlyList<PlaceSummaryDto> Places,
    IReadOnlyList<EventSummaryDto> Events);
