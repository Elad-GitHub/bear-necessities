using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.Services.Trips;

/// <summary>
/// The distance rules every trip plan follows, whichever planner made it:
/// a day stays within one small area, the next day moves on by no more than a day's drive,
/// and short trips stay in a single region. A country-wide request is planned around the
/// region that best matches the traveller rather than spread across the country.
/// </summary>
public static class TripGeography
{
    /// <summary>Every stop of a day lies within this distance of the day's first stop.</summary>
    public const double DayRadiusKm = 20;
    /// <summary>The first stop of a day lies within this distance of the previous day's first stop.</summary>
    public const double MaxDayHopKm = 100;
    /// <summary>Trips up to this many days stay in one region.</summary>
    public const int SingleRegionMaxDays = 4;
    /// <summary>The region of a short trip: every stop within this distance of the trip's first stop.</summary>
    public const double SingleRegionRadiusKm = 30;

    public static bool IsShortTrip(int days) => days <= SingleRegionMaxDays;

    public static double DistanceKm(Place from, Place to) => CoordinatesOf(from).DistanceKmTo(CoordinatesOf(to));

    public static double DistanceKm(Coordinates from, Place to) => from.DistanceKmTo(CoordinatesOf(to));

    public static Coordinates CoordinatesOf(Place place) => Coordinates.Create(place.Latitude, place.Longitude);

    /// <summary>
    /// Where the trip starts: the place with the most interest matches around it within a day's area.
    /// A city or point request looks only near that point; a country-wide request looks everywhere.
    /// </summary>
    public static Place? FindAnchor(IReadOnlyList<Place> pool, TripInterests interests, Coordinates? start, bool isCountryWide)
    {
        if (pool.Count == 0) return null;

        var nearStart = start is null || isCountryWide
            ? pool
            : pool.Where(place => DistanceKm(start, place) <= DayRadiusKm).ToList();
        if (nearStart.Count == 0 && start is not null)
            nearStart = [pool.MinBy(place => DistanceKm(start, place))!];

        return nearStart
            .OrderByDescending(candidate => AreaScore(candidate, pool, interests))
            .ThenBy(candidate => start is null ? 0 : DistanceKm(start, candidate))
            .First();
    }

    // Covering another of the traveller's interests outweighs any number of extra places for one they already have
    private const int DistinctInterestWeight = 100;

    /// <summary>How much of what the traveller wants lies within a day's area around a place: variety first, then amount.</summary>
    public static int AreaScore(Place center, IReadOnlyList<Place> pool, TripInterests interests)
    {
        var nearby = pool.Where(place => DistanceKm(center, place) <= DayRadiusKm).ToList();
        var distinctInterests = interests.Wanted.Count(interestId =>
            nearby.Any(place => place.Categories.Any(category => category.Id.Value == interestId)));
        return distinctInterests * DistinctInterestWeight + nearby.Sum(place => Math.Max(1, interests.MatchCount(place)));
    }

    /// <summary>
    /// Drops stops and days that break the rules, keeping the plan's order; used on answers from an AI planner.
    /// </summary>
    public static IReadOnlyList<PlannedDay> Enforce(IReadOnlyList<PlannedDay> days, int requestedDays)
    {
        var kept = new List<PlannedDay>();
        Place? tripStart = null;
        Place? previousDayStart = null;

        foreach (var day in days)
        {
            if (day.Stops.Count == 0) continue;
            var dayStart = day.Stops[0].Place;
            if (previousDayStart is not null && DistanceKm(previousDayStart, dayStart) > MaxDayHopKm) continue;
            tripStart ??= dayStart;
            if (IsShortTrip(requestedDays) && DistanceKm(tripStart, dayStart) > SingleRegionRadiusKm) continue;

            var stops = day.Stops
                .Where(stop => DistanceKm(dayStart, stop.Place) <= DayRadiusKm)
                .Where(stop => !IsShortTrip(requestedDays) || DistanceKm(tripStart, stop.Place) <= SingleRegionRadiusKm)
                .ToList();
            kept.Add(day with { Stops = stops });
            previousDayStart = dayStart;
        }
        return kept;
    }
}
