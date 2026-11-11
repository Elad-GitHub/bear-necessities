using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.UseCases.Places;

/// <summary>Turns the map's "When?" days into a UTC range covering whole days.</summary>
public static class MapDates
{
    public static DateRange? ToRange(DateOnly? from, DateOnly? to)
    {
        if (from is null && to is null) return null;
        var start = (from ?? to)!.Value;
        var end = (to ?? from)!.Value;
        if (end < start) (start, end) = (end, start);
        return new DateRange(start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), end.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc));
    }
}
