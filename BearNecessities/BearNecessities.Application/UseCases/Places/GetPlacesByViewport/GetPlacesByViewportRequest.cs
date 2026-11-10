namespace BearNecessities.Application.UseCases.Places.GetPlacesByViewport;

public record GetPlacesByViewportRequest(
    decimal NorthEastLat,
    decimal NorthEastLng,
    decimal SouthWestLat,
    decimal SouthWestLng,
    IEnumerable<Guid>? CategoryIds = null,
    int PageSize = 20,
    Guid? Cursor = null);
