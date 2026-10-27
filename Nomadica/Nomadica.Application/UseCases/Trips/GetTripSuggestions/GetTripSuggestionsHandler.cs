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
    private readonly IUnitOfWork _unitOfWork;

    public GetTripSuggestionsHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetTripSuggestionsResponse> HandleAsync(GetTripSuggestionsRequest request, CancellationToken cancellationToken = default)
    {
        var places = await TripCandidateSearch.FindAsync(
            _unitOfWork, request.Country, request.Latitude, request.Longitude,
            request.RadiusKm, request.CategoryIds, cancellationToken);

        var events = await _unitOfWork.Events.FindUpcomingByPlacesAsync(
            places.Select(p => p.Id), cancellationToken);

        return new GetTripSuggestionsResponse(
            Places: places.Select(TripCandidateSearch.ToSummary).ToList(),
            Events: events.Select(MapEvent).ToList());
    }

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
