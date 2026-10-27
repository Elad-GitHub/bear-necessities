using Nomadica.Application.Common;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Interfaces;

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
            Events: events.Select(EventMapping.ToSummaryDto).ToList());
    }
}
