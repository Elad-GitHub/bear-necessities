using Nomadica.Application.DTOs;
using Nomadica.Application.DTOs.Places;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Enums;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Application.UseCases.Places.GetPlacesByViewport;

public sealed class GetPlacesByViewportHandler
    : IRequestHandler<GetPlacesByViewportRequest, GetPlacesByViewportResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPlacesByViewportHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetPlacesByViewportResponse> HandleAsync(
        GetPlacesByViewportRequest request,
        CancellationToken cancellationToken)
    {
        var viewport = new GeoViewport(
            Coordinates.Create(request.NorthEastLat, request.NorthEastLng),
            Coordinates.Create(request.SouthWestLat, request.SouthWestLng));

        var categoryIds = request.CategoryIds?
            .Select(id => new CategoryId(id))
            .ToList();

        var (places, totalCount) = await _unitOfWork.Places.FindByViewportPagedAsync(
            viewport, categoryIds, request.Cursor, request.PageSize, cancellationToken);

        if (places.Count == 0)
            return new GetPlacesByViewportResponse([], null, totalCount);

        var placeGuids = places.Select(p => p.Id.Value).ToList();
        var allMedia = await _unitOfWork.Media.GetByOwnersAsync(
            MediaOwnerType.Place, placeGuids, cancellationToken);

        // GetByOwnersAsync orders by SortOrder; first per place is the primary image.
        var thumbnailByPlaceId = allMedia
            .GroupBy(m => m.OwnerId)
            .ToDictionary(g => g.Key, g => g.First().ThumbnailUrl);

        var items = places
            .Select(p => new PlaceSummaryDto(
                Id: p.Id.Value,
                Name: p.Name,
                Latitude: p.Latitude,
                Longitude: p.Longitude,
                H3IndexRes8: p.H3IndexRes8,
                City: p.City,
                Country: p.Country,
                Status: p.Status,
                AverageRating: null, // requires a dedicated review-aggregate query; not yet implemented
                ReviewCount: 0,
                CategoryIds: p.Categories.Select(c => c.Id.Value).ToList(),
                ThumbnailUrl: thumbnailByPlaceId.GetValueOrDefault(p.Id.Value),
                IsPermanent: p.IsPermanent,
                Description: p.Description))
            .ToList();

        var nextCursor = places.Count == request.PageSize
            ? places[^1].Id.Value
            : (Guid?)null;

        return new GetPlacesByViewportResponse(items, nextCursor, totalCount);
    }
}
