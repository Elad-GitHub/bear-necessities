namespace Nomadica.Domain.Interfaces;

public interface IOccurrenceExpander
{
    IReadOnlyList<(DateTime StartUtc, DateTime? EndUtc)> ExpandOccurrences(
        string rrule,
        DateTime dtStart,
        string timezone,
        int daysAhead);
}
