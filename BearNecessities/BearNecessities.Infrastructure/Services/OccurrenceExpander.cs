using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using BearNecessities.Domain.Interfaces;

namespace BearNecessities.Infrastructure.Services;

public sealed class OccurrenceExpander : IOccurrenceExpander
{
    private const int MaxOccurrences = 500;

    public IReadOnlyList<(DateTime StartUtc, DateTime? EndUtc)> ExpandOccurrences(
        string rrule,
        DateTime dtStart,
        string timezone,
        int daysAhead)
    {
        var calEvent = new CalendarEvent
        {
            DtStart = new CalDateTime(dtStart, timezone),
            RecurrenceRule = new RecurrencePattern(rrule)
        };

        var rangeEndUtc = dtStart.AddDays(daysAhead);
        var calStart = new CalDateTime(dtStart, timezone);

        return calEvent.GetOccurrences(calStart)
            .TakeWhile(o => o.Period.StartTime.AsUtc <= rangeEndUtc)
            .Take(MaxOccurrences)
            .Select(o => (
                StartUtc: o.Period.StartTime.AsUtc,
                EndUtc: o.Period.EndTime is not null && o.Period.EndTime != o.Period.StartTime
                    ? (DateTime?)o.Period.EndTime.AsUtc
                    : null))
            .ToList();
    }
}
