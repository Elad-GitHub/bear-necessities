using Nomadica.Application.DTOs.Places;

namespace Nomadica.Application.UseCases.Places.GetPlacesByViewport;

public record GetPlacesByViewportResponse(
    IReadOnlyList<PlaceSummaryDto> Items,
    Guid? NextCursor,
    int TotalCount);
