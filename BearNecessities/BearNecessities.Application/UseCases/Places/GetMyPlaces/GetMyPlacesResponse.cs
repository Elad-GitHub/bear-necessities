using BearNecessities.Application.DTOs.Places;

namespace BearNecessities.Application.UseCases.Places.GetMyPlaces;

public record GetMyPlacesResponse(IReadOnlyList<PlaceSummaryDto> Items);
