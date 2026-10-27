using Nomadica.Application.DTOs.Places;

namespace Nomadica.Application.UseCases.Places.SearchPlaces;

public record SearchPlacesResponse(IReadOnlyList<PlaceSummaryDto> Items);
