namespace Nomadica.Application.UseCases.Places.GetPlaceDensity;

public record GetPlaceDensityRequest(
    decimal NorthEastLat,
    decimal NorthEastLng,
    decimal SouthWestLat,
    decimal SouthWestLng,
    double Zoom,
    List<Guid>? CategoryIds = null);

public record PlaceDensityCellDto(decimal Latitude, decimal Longitude, int Count);

public record GetPlaceDensityResponse(IReadOnlyList<PlaceDensityCellDto> Cells, int TotalCount);
