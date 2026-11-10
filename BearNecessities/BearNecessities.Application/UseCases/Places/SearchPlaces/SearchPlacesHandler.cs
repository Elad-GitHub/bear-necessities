using BearNecessities.Application.DTOs.Places;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Interfaces;

namespace BearNecessities.Application.UseCases.Places.SearchPlaces;

public sealed class SearchPlacesHandler : IRequestHandler<SearchPlacesRequest, SearchPlacesResponse>
{
    private const int MaxLimit = 20;
    private readonly IUnitOfWork _unitOfWork;

    public SearchPlacesHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SearchPlacesResponse> HandleAsync(SearchPlacesRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query) || request.Query.Trim().Length < 2)
            return new SearchPlacesResponse([]);

        var limit = Math.Clamp(request.Limit, 1, MaxLimit);
        var places = await _unitOfWork.Places.SearchByTextAsync(request.Query, limit, cancellationToken);

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
                AverageRating: null,
                ReviewCount: 0,
                CategoryIds: p.Categories.Select(c => c.Id.Value).ToList(),
                ThumbnailUrl: null,
                IsPermanent: p.IsPermanent))
            .ToList();

        return new SearchPlacesResponse(items);
    }
}
