using BearNecessities.Application.DTOs.Places;

namespace BearNecessities.Application.UseCases.Trips.PlanItinerary;

public record PlanItineraryRequest(
    string? Country,
    decimal? Latitude,
    decimal? Longitude,
    List<Guid> CategoryIds,
    int Days = 3,
    int StopsPerDay = 4,
    double RadiusKm = 150,
    string? Wish = null);

/// <param name="Reasons">Why each stop was picked, keyed by place id; stops without a reason are left out.</param>
public record ItineraryDayDto(
    int DayNumber,
    IReadOnlyList<PlaceSummaryDto> Stops,
    double DistanceKm,
    string? Title,
    IReadOnlyDictionary<Guid, string> Reasons);

public record PlanItineraryResponse(IReadOnlyList<ItineraryDayDto> Days, int TotalStops, string Planner);
