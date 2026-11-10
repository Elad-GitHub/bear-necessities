using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.UseCases.Trips.PlanItinerary;

public sealed class PlanItineraryHandler : IRequestHandler<PlanItineraryRequest, PlanItineraryResponse>
{
    public const int MinCategories = 3;
    public const int MaxDays = 14;
    public const int MaxStopsPerDay = 8;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IItineraryPlanner _planner;

    public PlanItineraryHandler(IUnitOfWork unitOfWork, IItineraryPlanner planner)
    {
        _unitOfWork = unitOfWork;
        _planner = planner;
    }

    public async Task<PlanItineraryResponse> HandleAsync(PlanItineraryRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);

        var candidates = await TripCandidateSearch.FindAsync(
            _unitOfWork, request.Country, request.Latitude, request.Longitude,
            request.RadiusKm, request.CategoryIds, cancellationToken);

        var start = request.Latitude.HasValue && request.Longitude.HasValue
            ? Coordinates.Create(request.Latitude.Value, request.Longitude.Value)
            : null;

        var plannedDays = await _planner.PlanAsync(
            new ItineraryPlanningInput(candidates, request.CategoryIds, start, request.Days, request.StopsPerDay),
            cancellationToken);

        var days = plannedDays
            .Select((stops, index) => new ItineraryDayDto(
                DayNumber: index + 1,
                Stops: stops.Select(TripCandidateSearch.ToSummary).ToList(),
                DistanceKm: Math.Round(MeasureDayKm(stops), 1)))
            .ToList();

        return new PlanItineraryResponse(days, days.Sum(day => day.Stops.Count), _planner.Name);
    }

    private static double MeasureDayKm(IReadOnlyList<Place> stops) =>
        stops.Zip(stops.Skip(1), (from, to) =>
                Coordinates.Create(from.Latitude, from.Longitude)
                    .DistanceKmTo(Coordinates.Create(to.Latitude, to.Longitude)))
            .Sum();

    private static void Validate(PlanItineraryRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.CategoryIds is null || request.CategoryIds.Distinct().Count() < MinCategories)
            errors["categoryIds"] = [$"Pick at least {MinCategories} subcategories."];
        if (request.Days is < 1 or > MaxDays)
            errors["days"] = [$"Days must be between 1 and {MaxDays}."];
        if (request.StopsPerDay is < 1 or > MaxStopsPerDay)
            errors["stopsPerDay"] = [$"Stops per day must be between 1 and {MaxStopsPerDay}."];

        if (errors.Count > 0)
            throw new ValidationException(errors);
    }
}
