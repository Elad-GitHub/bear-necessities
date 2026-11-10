namespace BearNecessities.Application.UseCases.Places.GetPlaceDensity;

public record GetPlaceDensityRequest(
    decimal NorthEastLat,
    decimal NorthEastLng,
    decimal SouthWestLat,
    decimal SouthWestLng,
    double Zoom,
    List<Guid>? CategoryIds = null);

/// <param name="Bounds">[west, south, east, north] of the places in the cell, for zoom-to-fit on click.</param>
public record PlaceDensityCellDto(decimal Latitude, decimal Longitude, int Count, decimal[] Bounds);

public record GetPlaceDensityResponse(IReadOnlyList<PlaceDensityCellDto> Cells, int TotalCount);
