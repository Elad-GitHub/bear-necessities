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
    public const int MaxWishLength = 500;

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
        var wish = string.IsNullOrWhiteSpace(request.Wish) ? null : request.Wish.Trim();

        var candidates = await TripCandidateSearch.FindAsync(
            _unitOfWork, request.Country, request.Latitude, request.Longitude,
            request.RadiusKm, request.CategoryIds, cancellationToken);

        var start = request.Latitude.HasValue && request.Longitude.HasValue
            ? Coordinates.Create(request.Latitude.Value, request.Longitude.Value)
            : null;

        var plan = await _planner.PlanAsync(
            new ItineraryPlanningInput(candidates, request.CategoryIds ?? [], start, request.Days, request.StopsPerDay, wish,
                IsCountryWide: !string.IsNullOrWhiteSpace(request.Country)),
            cancellationToken);

        var days = plan.Days
            .Select((day, index) => new ItineraryDayDto(
                DayNumber: index + 1,
                Stops: day.Stops.Select(stop => TripCandidateSearch.ToSummary(stop.Place)).ToList(),
                DistanceKm: Math.Round(MeasureDayKm(day.Stops.Select(stop => stop.Place).ToList()), 1),
                Title: day.Title,
                Reasons: day.Stops
                    .Where(stop => !string.IsNullOrWhiteSpace(stop.Reason))
                    .ToDictionary(stop => stop.Place.Id.Value, stop => stop.Reason!)))
            .ToList();

        return new PlanItineraryResponse(days, days.Sum(day => day.Stops.Count), plan.Planner);
    }

    private static double MeasureDayKm(IReadOnlyList<Place> stops) =>
        stops.Zip(stops.Skip(1), (from, to) =>
                Coordinates.Create(from.Latitude, from.Longitude)
                    .DistanceKmTo(Coordinates.Create(to.Latitude, to.Longitude)))
            .Sum();

    private static void Validate(PlanItineraryRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var hasWish = !string.IsNullOrWhiteSpace(request.Wish);

        // A written wish says what the traveller wants, so the subcategory minimum only applies without one
        if (!hasWish && (request.CategoryIds is null || request.CategoryIds.Distinct().Count() < MinCategories))
            errors["categoryIds"] = [$"Pick at least {MinCategories} subcategories, or describe your trip."];
        if (request.Wish?.Length > MaxWishLength)
            errors["wish"] = [$"Keep the trip description under {MaxWishLength} characters."];
        if (request.Days is < 1 or > MaxDays)
            errors["days"] = [$"Days must be between 1 and {MaxDays}."];
        if (request.StopsPerDay is < 1 or > MaxStopsPerDay)
            errors["stopsPerDay"] = [$"Stops per day must be between 1 and {MaxStopsPerDay}."];

        if (errors.Count > 0)
            throw new ValidationException(errors);
    }
}
