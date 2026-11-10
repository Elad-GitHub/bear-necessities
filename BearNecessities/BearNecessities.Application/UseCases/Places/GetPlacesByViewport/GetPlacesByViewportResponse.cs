using BearNecessities.Application.DTOs.Places;

namespace BearNecessities.Application.UseCases.Places.GetPlacesByViewport;

public record GetPlacesByViewportResponse(
    IReadOnlyList<PlaceSummaryDto> Items,
    Guid? NextCursor,
    int TotalCount);
