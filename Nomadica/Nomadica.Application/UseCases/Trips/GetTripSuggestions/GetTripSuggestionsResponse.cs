using Nomadica.Application.DTOs.Events;
using Nomadica.Application.DTOs.Places;

namespace Nomadica.Application.UseCases.Trips.GetTripSuggestions;

public record GetTripSuggestionsResponse(
    IReadOnlyList<PlaceSummaryDto> Places,
    IReadOnlyList<EventSummaryDto> Events);
