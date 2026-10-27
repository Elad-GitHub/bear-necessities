using Nomadica.Domain.Entities;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Application.Interfaces;

public sealed record ItineraryPlanningInput(
    IReadOnlyList<Place> Candidates,
    IReadOnlyList<Guid> CategoryIds,
    Coordinates? Start,
    int Days,
    int StopsPerDay);

/// <summary>Turns candidate places into an ordered, day-by-day route. Implementations can be swapped (e.g. an LLM planner with this as fallback).</summary>
public interface IItineraryPlanner
{
    string Name { get; }

    Task<IReadOnlyList<IReadOnlyList<Place>>> PlanAsync(ItineraryPlanningInput input, CancellationToken cancellationToken = default);
}
