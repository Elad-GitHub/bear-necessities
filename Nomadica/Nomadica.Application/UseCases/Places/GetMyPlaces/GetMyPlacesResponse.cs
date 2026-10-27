using Nomadica.Application.DTOs.Places;

namespace Nomadica.Application.UseCases.Places.GetMyPlaces;

public record GetMyPlacesResponse(IReadOnlyList<PlaceSummaryDto> Items);
