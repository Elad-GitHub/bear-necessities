namespace Nomadica.Application.UseCases.Trips.GetTripSuggestions;

public record GetTripSuggestionsRequest(
    string? Country,
    decimal? Latitude,
    decimal? Longitude,
    double RadiusKm = 150,
    List<Guid>? CategoryIds = null);
