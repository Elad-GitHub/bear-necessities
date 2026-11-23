using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.Services.Trips;

/// <summary>
/// Deterministic planner, no AI: reads the traveller's interests from the picked subcategories and the free-text
/// wish, starts in the area that best matches them, and builds one day at a time within the
/// <see cref="TripGeography"/> distance rules, taking the nearest place of the next interest in turn
/// (places that match several interests count as closer). Each day and stop gets a title or reason.
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

        var tripStart = TripGeography.FindAnchor(remaining, interests, input.Start, input.IsCountryWide, input.Days)!;
        var isShortTrip = TripGeography.IsShortTrip(input.Days);
        bool InRegion(Place place) =>
            !isShortTrip || TripGeography.DistanceKm(tripStart, place) <= TripGeography.SingleRegionRadiusKm;

        var rotation = new Queue<Guid>(interests.Wanted);
        var days = new List<PlannedDay>();
        Place? dayStart = tripStart;
        while (dayStart is not null && days.Count < input.Days)
        {
            var center = dayStart;
            var stops = BuildDayStops(center, remaining, rotation, interests,
                place => InRegion(place) && TripGeography.DistanceKm(center, place) <= TripGeography.DayRadiusKm,
                input.StopsPerDay);
            days.Add(BuildDay(stops, interests));

            dayStart = NextDayStart(center, remaining, interests, InRegion);
        }
        return new PlannedItinerary(days, PlannerName);
    }

    // The day begins at its center, then takes the nearest place of the next interest within the day's area,
    // one place of each kind per day ("one ayurveda, not four")
    private static List<Place> BuildDayStops(
        Place center, List<Place> remaining, Queue<Guid> rotation, TripInterests interests,
        Func<Place, bool> isInDayArea, int stopsPerDay)
    {
        remaining.Remove(center);
        MoveToBack(rotation, center);
        var stops = new List<Place> { center };
        var kindsToday = interests.KindsOf(center).ToHashSet();
        var position = TripGeography.CoordinatesOf(center);
        bool IsEligible(Place place) => isInDayArea(place) && !interests.KindsOf(place).Any(kindsToday.Contains);

        while (stops.Count < stopsPerDay)
        {
            var next = TakeNearestInNextInterest(remaining, rotation, interests, position, IsEligible)
                ?? TakeNearest(remaining, interests, position, IsEligible);
            if (next is null) break;
            stops.Add(next);
            kindsToday.UnionWith(interests.KindsOf(next));
            position = TripGeography.CoordinatesOf(next);
        }
        return stops;
    }

    // Tomorrow starts within a day's drive, where the most of what the traveller wants is left
    private static Place? NextDayStart(Place today, IReadOnlyList<Place> remaining, TripInterests interests, Func<Place, bool> isInRegion) =>
        remaining
            .Where(place => isInRegion(place) && TripGeography.DistanceKm(today, place) <= TripGeography.MaxDayHopKm)
            .OrderByDescending(place => TripGeography.AreaScore(place, remaining, interests))
            .ThenBy(place => TripGeography.DistanceKm(today, place))
            .FirstOrDefault();

    // The day's first stop already serves one interest, so the rotation moves on to the next
    private static void MoveToBack(Queue<Guid> rotation, Place place)
    {
        var order = rotation.ToList();
        var served = order.FindIndex(categoryId => place.Categories.Any(category => category.Id.Value == categoryId));
        if (served < 0) return;

        var categoryId = order[served];
        order.RemoveAt(served);
        order.Add(categoryId);
        rotation.Clear();
        order.ForEach(rotation.Enqueue);
    }

    // An interest with nothing left in the area drops out of today's turn so it can't stall the day
    private static Place? TakeNearestInNextInterest(
        List<Place> remaining, Queue<Guid> rotation, TripInterests interests, Coordinates position, Func<Place, bool> isInDayArea)
    {
        for (var tries = rotation.Count; tries > 0; tries--)
        {
            var categoryId = rotation.Dequeue();
            rotation.Enqueue(categoryId);
            var match = TakeNearest(remaining, interests, position,
                place => isInDayArea(place) && place.Categories.Any(category => category.Id.Value == categoryId));
            if (match is not null)
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
            var score = TripGeography.DistanceKm(position, remaining[i]) / (1 + ExtraMatchPull * extraMatches);
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
}
