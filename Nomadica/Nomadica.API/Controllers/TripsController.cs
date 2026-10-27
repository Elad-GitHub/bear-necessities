using Microsoft.AspNetCore.Mvc;
using Nomadica.Application.UseCases.Trips.GetTripSuggestions;

namespace Nomadica.API.Controllers;

public class TripsController : BaseApiController
{
    private readonly GetTripSuggestionsHandler _suggestionsHandler;

    public TripsController(GetTripSuggestionsHandler suggestionsHandler)
    {
        _suggestionsHandler = suggestionsHandler;
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
