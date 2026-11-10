using BearNecessities.Application.DTOs.Places;

namespace BearNecessities.Application.UseCases.Places.SearchPlaces;

public record SearchPlacesResponse(IReadOnlyList<PlaceSummaryDto> Items);
