using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.Services.Trips;

/// <summary>
/// Deterministic planner: walks outward from the start, rotating through the chosen categories and
/// always taking the nearest unvisited place of the next category, then splits the walk into days.
/// </summary>
public sealed class NearestNeighborItineraryPlanner : IItineraryPlanner
{
    public string Name => "nearest-neighbor";

    public Task<IReadOnlyList<IReadOnlyList<Place>>> PlanAsync(ItineraryPlanningInput input, CancellationToken cancellationToken = default)
    {
        var remaining = input.Candidates.ToList();
        if (remaining.Count == 0 || input.Days < 1 || input.StopsPerDay < 1)
            return Task.FromResult<IReadOnlyList<IReadOnlyList<Place>>>([]);

        var totalStops = Math.Min(input.Days * input.StopsPerDay, remaining.Count);
        var position = input.Start ?? Centroid(remaining);
        var categoryRotation = new Queue<Guid>(input.CategoryIds.Distinct());
        var route = new List<Place>(totalStops);

        while (route.Count < totalStops)
        {
            var next = TakeNearestInNextCategory(remaining, categoryRotation, position)
                ?? TakeNearest(remaining, position, _ => true)!;
            route.Add(next);
            position = CoordinatesOf(next);
        }

        IReadOnlyList<IReadOnlyList<Place>> days = route
            .Chunk(input.StopsPerDay)
            .Select(dayStops => (IReadOnlyList<Place>)dayStops.ToList())
            .ToList();
        return Task.FromResult(days);
    }

    // A category with nothing left nearby drops out of the rotation so it can't stall the walk
    private static Place? TakeNearestInNextCategory(List<Place> remaining, Queue<Guid> categoryRotation, Coordinates position)
    {
        while (categoryRotation.Count > 0)
        {
            var categoryId = categoryRotation.Dequeue();
            var match = TakeNearest(remaining, position,
                place => place.Categories.Any(category => category.Id.Value == categoryId));
            if (match is null)
                continue;

            categoryRotation.Enqueue(categoryId);
            return match;
        }
        return null;
    }

    private static Place? TakeNearest(List<Place> remaining, Coordinates position, Func<Place, bool> isEligible)
    {
        var nearestIndex = -1;
        var nearestDistance = double.MaxValue;
        for (var i = 0; i < remaining.Count; i++)
        {
            if (!isEligible(remaining[i]))
                continue;

            var distance = position.DistanceKmTo(CoordinatesOf(remaining[i]));
            if (distance >= nearestDistance)
                continue;

            nearestDistance = distance;
            nearestIndex = i;
        }

        if (nearestIndex < 0)
            return null;

        var nearest = remaining[nearestIndex];
        remaining.RemoveAt(nearestIndex);
        return nearest;
    }

    private static Coordinates Centroid(IReadOnlyCollection<Place> places) =>
        Coordinates.Create(places.Average(place => place.Latitude), places.Average(place => place.Longitude));

    private static Coordinates CoordinatesOf(Place place) => Coordinates.Create(place.Latitude, place.Longitude);
}
