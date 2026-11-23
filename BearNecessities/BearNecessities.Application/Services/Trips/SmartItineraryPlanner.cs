using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.Services.Trips;

/// <summary>
/// Deterministic planner, no AI: reads the traveller's interests from the picked subcategories and the free-text
/// wish, walks outward from the start taking the nearest place of the next interest in turn (places that match
/// several interests count as closer), splits the walk into days, and names each day and stop.
/// </summary>
public sealed class SmartItineraryPlanner : IItineraryPlanner
{
    public const string PlannerName = "smart";

    // A place matching two interests is treated as this much closer per extra match
    private const double ExtraMatchPull = 0.5;
    private const int TitleThemes = 2;

    public string Name => PlannerName;

    public Task<PlannedItinerary> PlanAsync(ItineraryPlanningInput input, CancellationToken cancellationToken = default) =>
        Task.FromResult(Plan(input));

    public PlannedItinerary Plan(ItineraryPlanningInput input)
    {
        var interests = TripInterests.From(input.Candidates, input.CategoryIds, input.Wish);
        var remaining = input.Candidates
            .Where(place => !interests.IsRuledOut(place) && (interests.IsEmpty || interests.MatchCount(place) > 0))
            .ToList();
        if (remaining.Count == 0 || input.Days < 1 || input.StopsPerDay < 1)
            return new PlannedItinerary([], PlannerName);

        var route = WalkRoute(remaining, interests, input);
        var days = route
            .Chunk(input.StopsPerDay)
            .Select(dayStops => BuildDay(dayStops, interests))
            .ToList();
        return new PlannedItinerary(days, PlannerName);
    }

    private static List<Place> WalkRoute(List<Place> remaining, TripInterests interests, ItineraryPlanningInput input)
    {
        var totalStops = Math.Min(input.Days * input.StopsPerDay, remaining.Count);
        var position = input.Start ?? Centroid(remaining);
        var rotation = new Queue<Guid>(interests.Wanted);
        var route = new List<Place>(totalStops);

        while (route.Count < totalStops)
        {
            var next = TakeNearestInNextInterest(remaining, rotation, interests, position)
                ?? TakeNearest(remaining, interests, position, _ => true)!;
            route.Add(next);
            position = CoordinatesOf(next);
        }
        return route;
    }

    // An interest with nothing left nearby drops out of the rotation so it can't stall the walk
    private static Place? TakeNearestInNextInterest(List<Place> remaining, Queue<Guid> rotation, TripInterests interests, Coordinates position)
    {
        while (rotation.Count > 0)
        {
            var categoryId = rotation.Dequeue();
            var match = TakeNearest(remaining, interests, position,
                place => place.Categories.Any(category => category.Id.Value == categoryId));
            if (match is null)
                continue;

            rotation.Enqueue(categoryId);
            return match;
        }
        return null;
    }

    private static Place? TakeNearest(List<Place> remaining, TripInterests interests, Coordinates position, Func<Place, bool> isEligible)
    {
        var bestIndex = -1;
        var bestScore = double.MaxValue;
        for (var i = 0; i < remaining.Count; i++)
        {
            if (!isEligible(remaining[i]))
                continue;

            var extraMatches = Math.Max(0, interests.MatchCount(remaining[i]) - 1);
            var score = position.DistanceKmTo(CoordinatesOf(remaining[i])) / (1 + ExtraMatchPull * extraMatches);
            if (score >= bestScore)
                continue;

            bestScore = score;
            bestIndex = i;
        }

        if (bestIndex < 0)
            return null;

        var best = remaining[bestIndex];
        remaining.RemoveAt(bestIndex);
        return best;
    }

    private static PlannedDay BuildDay(IReadOnlyList<Place> stops, TripInterests interests)
    {
        var plannedStops = stops
            .Select(place => new PlannedStop(place, Reason(interests.MatchedNames(place))))
            .ToList();
        return new PlannedDay(Title(stops, interests), plannedStops);
    }

    private static string? Reason(IReadOnlyList<string> matchedNames) =>
        matchedNames.Count == 0 ? null : $"For your interest in {JoinNames(matchedNames)}";

    // "Ubud · Yoga · Sound Healing": the day's most common town and its leading themes
    private static string? Title(IReadOnlyList<Place> stops, TripInterests interests)
    {
        var town = stops
            .Select(place => place.City)
            .Where(city => !string.IsNullOrWhiteSpace(city))
            .GroupBy(city => city!)
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key)
            .FirstOrDefault();
        var themes = stops
            .SelectMany(interests.MatchedNames)
            .GroupBy(name => name)
            .OrderByDescending(group => group.Count())
            .Take(TitleThemes)
            .Select(group => group.Key)
            .ToList();

        // Subcategory names already contain "&" ("Weaving, Macramé & Fiber Arts"), so themes are dot-separated
        var theme = themes.Count == 0 ? null : string.Join(" · ", themes);
        return (town, theme) switch
        {
            (null, null) => null,
            (null, _) => theme,
            (_, null) => town,
            _ => $"{town} · {theme}"
        };
    }

    private static string JoinNames(IReadOnlyList<string> names) =>
        names.Count == 1 ? names[0] : $"{string.Join(", ", names.Take(names.Count - 1))} and {names[^1]}";

    private static Coordinates Centroid(IReadOnlyCollection<Place> places) =>
        Coordinates.Create(places.Average(place => place.Latitude), places.Average(place => place.Longitude));

    private static Coordinates CoordinatesOf(Place place) => Coordinates.Create(place.Latitude, place.Longitude);
}
