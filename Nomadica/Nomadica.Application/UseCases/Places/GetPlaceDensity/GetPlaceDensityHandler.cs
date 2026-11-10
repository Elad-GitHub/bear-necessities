using Nomadica.Application.Interfaces;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Application.UseCases.Places.GetPlaceDensity;

public sealed class GetPlaceDensityHandler : IRequestHandler<GetPlaceDensityRequest, GetPlaceDensityResponse>
{
    // A cell spans roughly a quarter of a 256px map tile at the requested zoom: ~15° at zoom 2, ~0.5° at zoom 7
    private const double CellDegreesAtZoomZero = 60.0;
    private const double MinZoom = 0;
    private const double MaxZoom = 14;

    private readonly IUnitOfWork _unitOfWork;

    public GetPlaceDensityHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public static decimal CellSizeForZoom(double zoom) =>
        (decimal)(CellDegreesAtZoomZero / Math.Pow(2, Math.Clamp(Math.Floor(zoom), MinZoom, MaxZoom)));

    public async Task<GetPlaceDensityResponse> HandleAsync(GetPlaceDensityRequest request, CancellationToken cancellationToken = default)
    {
        var viewport = new GeoViewport(
            Coordinates.Create(request.NorthEastLat, request.NorthEastLng),
            Coordinates.Create(request.SouthWestLat, request.SouthWestLng));

        var cells = await _unitOfWork.Places.GetDensityAsync(
            viewport, request.CategoryIds, CellSizeForZoom(request.Zoom), cancellationToken);

        var dtos = cells
            .Select(cell => new PlaceDensityCellDto(Math.Round(cell.Latitude, 5), Math.Round(cell.Longitude, 5), cell.Count))
            .ToList();
        return new GetPlaceDensityResponse(dtos, dtos.Sum(cell => cell.Count));
    }
}
