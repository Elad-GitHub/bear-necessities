using Microsoft.AspNetCore.Mvc;
using Nomadica.Application.UseCases.Trips.GetTripSuggestions;
using Nomadica.Application.UseCases.Trips.PlanItinerary;

namespace Nomadica.API.Controllers;

public class TripsController : BaseApiController
{
    private readonly GetTripSuggestionsHandler _suggestionsHandler;
    private readonly PlanItineraryHandler _itineraryHandler;

    public TripsController(GetTripSuggestionsHandler suggestionsHandler, PlanItineraryHandler itineraryHandler)
    {
        _suggestionsHandler = suggestionsHandler;
        _itineraryHandler = itineraryHandler;
    }

    [HttpGet("itinerary")]
    public async Task<IActionResult> PlanItinerary(
        [FromQuery] string? country,
        [FromQuery] decimal? latitude,
        [FromQuery] decimal? longitude,
        [FromQuery] Guid[]? categoryIds,
        [FromQuery] int days = 3,
        [FromQuery] int stopsPerDay = 4,
        [FromQuery] double radiusKm = 150,
        CancellationToken cancellationToken = default)
    {
        var request = new PlanItineraryRequest(
            country, latitude, longitude, categoryIds?.ToList() ?? [], days, stopsPerDay, radiusKm);
        var response = await _itineraryHandler.HandleAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions(
        [FromQuery] string? country,
        [FromQuery] decimal? latitude,
        [FromQuery] decimal? longitude,
        [FromQuery] double radiusKm = 150,
        [FromQuery] Guid[]? categoryIds = null,
        CancellationToken cancellationToken = default)
    {
        var request = new GetTripSuggestionsRequest(
            country, latitude, longitude, radiusKm, categoryIds?.ToList());

        var response = await _suggestionsHandler.HandleAsync(request, cancellationToken);
        return Ok(response);
    }
}
