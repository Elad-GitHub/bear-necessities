using Nomadica.Application.DTOs.Events;
using Nomadica.Application.DTOs.Places;
using Nomadica.Application.Exceptions;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Entities;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Application.UseCases.Trips.GetTripSuggestions;

public sealed class GetTripSuggestionsHandler : IRequestHandler<GetTripSuggestionsRequest, GetTripSuggestionsResponse>
{
    private const double MaxRadiusKm = 500;
    private readonly IUnitOfWork _unitOfWork;

    public GetTripSuggestionsHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetTripSuggestionsResponse> HandleAsync(GetTripSuggestionsRequest request, CancellationToken cancellationToken = default)
    {
        var places = await FindPlacesAsync(request, cancellationToken);

        var categoryFilter = request.CategoryIds?.ToHashSet();
        if (categoryFilter is { Count: > 0 })
        {
            places = places
                .Where(p => p.Categories.Any(c => categoryFilter.Contains(c.Id.Value)))
                .ToList();
        }

        var events = await _unitOfWork.Events.FindUpcomingByPlacesAsync(
            places.Select(p => p.Id), cancellationToken);

        return new GetTripSuggestionsResponse(
            Places: places.Select(MapPlace).ToList(),
            Events: events.Select(MapEvent).ToList());
    }

    private async Task<List<Place>> FindPlacesAsync(GetTripSuggestionsRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Country))
            return await _unitOfWork.Places.FindByCountryAsync(request.Country, cancellationToken);

        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            var radius = Math.Clamp(request.RadiusKm, 1, MaxRadiusKm);
            var center = Coordinates.Create(request.Latitude.Value, request.Longitude.Value);
            var nearby = await _unitOfWork.Places.FindNearbyAsync(center, radius, cancellationToken);
            return nearby.ToList();
        }

        throw new ValidationException(new Dictionary<string, string[]>
        {
            ["area"] = ["Provide either a country or a latitude/longitude center."]
        });
    }

    private static PlaceSummaryDto MapPlace(Place place) => new(
        Id: place.Id.Value,
        Name: place.Name,
        Latitude: place.Latitude,
        Longitude: place.Longitude,
        H3IndexRes8: place.H3IndexRes8,
        City: place.City,
        Country: place.Country,
        Status: place.Status,
        AverageRating: null,
        ReviewCount: 0,
        CategoryIds: place.Categories.Select(c => c.Id.Value).ToList(),
        ThumbnailUrl: null,
        IsPermanent: place.IsPermanent);

    private static EventSummaryDto MapEvent(Event evt) => new(
        Id: evt.Id.Value,
        Title: evt.Title,
        PlaceId: evt.PlaceId?.Value,
        Latitude: null,
        Longitude: null,
        StartAt: evt.StartAt,
        EndAt: evt.EndAt,
        EventType: evt.EventType,
        IsFree: evt.IsFree,
        Price: evt.Price,
        Currency: evt.Currency,
        Status: evt.Status,
        CategoryIds: evt.Categories.Select(c => c.Id.Value).ToList(),
        ThumbnailUrl: null,
        AverageRating: null);
}
