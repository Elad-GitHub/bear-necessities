using BearNecessities.Application.DTOs.Places;

namespace BearNecessities.Application.UseCases.Places.GetPendingPlaces;

public record GetPendingPlacesResponse(IReadOnlyList<PlaceSummaryDto> Items, Guid? NextCursor);
