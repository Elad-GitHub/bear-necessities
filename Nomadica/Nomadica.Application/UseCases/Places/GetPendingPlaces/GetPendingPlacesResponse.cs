using Nomadica.Application.DTOs.Places;

namespace Nomadica.Application.UseCases.Places.GetPendingPlaces;

public record GetPendingPlacesResponse(IReadOnlyList<PlaceSummaryDto> Items, Guid? NextCursor);
