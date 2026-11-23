using BearNecessities.Domain.Entities;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.Interfaces;

public sealed record ItineraryPlanningInput(
    IReadOnlyList<Place> Candidates,
    IReadOnlyList<Guid> CategoryIds,
    Coordinates? Start,
    int Days,
    int StopsPerDay,
    string? Wish = null,
    bool IsCountryWide = false);

/// <summary>One stop of a planned day, with a short line on why it was picked.</summary>
public sealed record PlannedStop(Place Place, string? Reason);

/// <summary>One day of a planned trip; the title names its area and theme.</summary>
public sealed record PlannedDay(string? Title, IReadOnlyList<PlannedStop> Stops);

/// <summary>A day-by-day route and the name of the planner that produced it.</summary>
public sealed record PlannedItinerary(IReadOnlyList<PlannedDay> Days, string Planner);

/// <summary>Turns candidate places into an ordered, day-by-day route. Implementations can be swapped (e.g. an LLM planner with this as fallback).</summary>
public interface IItineraryPlanner
{
    string Name { get; }

    Task<PlannedItinerary> PlanAsync(ItineraryPlanningInput input, CancellationToken cancellationToken = default);
}
