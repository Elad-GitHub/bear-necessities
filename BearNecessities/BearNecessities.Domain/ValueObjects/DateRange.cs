namespace BearNecessities.Domain.ValueObjects;

/// <summary>A span of days (UTC) someone is asking about, such as "this week" on the map.</summary>
public sealed record DateRange
{
    public DateTime From { get; }
    public DateTime To { get; }

    public DateRange(DateTime from, DateTime to)
    {
        if (to < from)
            throw new ArgumentException("The end of a date range cannot be before its start.", nameof(to));
        From = from;
        To = to;
    }
}
