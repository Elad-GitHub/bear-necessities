using Nomadica.Application.DTOs.Places;

namespace Nomadica.Application.UseCases.Trips.PlanItinerary;

public record PlanItineraryRequest(
    string? Country,
    decimal? Latitude,
    decimal? Longitude,
    List<Guid> CategoryIds,
    int Days = 3,
    int StopsPerDay = 4,
    double RadiusKm = 150);

public record ItineraryDayDto(int DayNumber, IReadOnlyList<PlaceSummaryDto> Stops, double DistanceKm);

public record PlanItineraryResponse(IReadOnlyList<ItineraryDayDto> Days, int TotalStops, string Planner);
