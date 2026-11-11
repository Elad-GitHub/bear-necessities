namespace BearNecessities.Domain.ValueObjects;

/// <summary>The soonest happening at a place. StartAt is null for an ongoing event with no dates.</summary>
public sealed record PlaceNextEvent(EventId EventId, string Title, DateTime? StartAt);
