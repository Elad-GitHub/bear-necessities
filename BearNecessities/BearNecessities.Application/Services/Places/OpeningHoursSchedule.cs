using System.Text.RegularExpressions;

namespace BearNecessities.Application.Services.Places;

/// <summary>Whether a place is open at a moment, and when it closes; ClosesAt is null when it never closes.</summary>
public readonly record struct OpenState(bool IsOpen, DateTime? ClosesAt);

/// <summary>
/// Reads the common subset of OpenStreetMap opening_hours ("Mo-Fr 09:00-18:00; Sa 10:00-14:00", "24/7",
/// "May-Oct 09:00-21:00", "Su off"). Anything outside that subset makes the whole value unknown, so a
/// place is never shown as open on a guess.
/// </summary>
public sealed partial class OpeningHoursSchedule
{
    private const int MinutesPerDay = 24 * 60;
    private const int DaysPerWeek = 7;
    private const int MonthsPerYear = 12;
    private static readonly string[] DayNames = ["Mo", "Tu", "We", "Th", "Fr", "Sa", "Su"];
    private static readonly string[] MonthNames = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

    private sealed record Rule(bool[] Months, bool[] Days, IReadOnlyList<(int Start, int End)> Ranges);

    private readonly IReadOnlyList<Rule> _rules;
    private readonly bool _isAlwaysOpen;

    private OpeningHoursSchedule(IReadOnlyList<Rule> rules, bool isAlwaysOpen)
    {
        _rules = rules;
        _isAlwaysOpen = isAlwaysOpen;
    }

    public static OpeningHoursSchedule? Parse(string? openingHours)
    {
        // Mappers often type an en dash where the syntax wants a hyphen
        var text = openingHours?.Trim().Replace('–', '-');
        if (string.IsNullOrEmpty(text))
            return null;
        if (text == "24/7")
            return new OpeningHoursSchedule([], isAlwaysOpen: true);

        var rules = new List<Rule>();
        foreach (var part in RuleSeparator().Split(text).Select(part => part.Trim()).Where(part => part.Length > 0))
        {
            // Holiday rules need a holiday calendar we don't have; the regular week still applies
            if (HolidaySelector().IsMatch(part))
                continue;
            var rule = ParseRule(part);
            if (rule is null)
                return null;
            rules.Add(rule);
        }
        return rules.Count == 0 ? null : new OpeningHoursSchedule(rules, isAlwaysOpen: false);
    }

    public OpenState StateAt(DateTime localTime)
    {
        if (_isAlwaysOpen)
            return new OpenState(true, null);

        var today = localTime.Date;
        var minute = (int)(localTime - today).TotalMinutes;
        foreach (var (start, end) in RangesOn(today))
        {
            if (minute >= start && minute < end)
                return new OpenState(true, today.AddMinutes(end));
        }

        // A range past midnight ("18:00-02:00") belongs to the day it started
        var yesterday = today.AddDays(-1);
        foreach (var (start, end) in RangesOn(yesterday).Where(range => range.End > MinutesPerDay))
        {
            if (minute + MinutesPerDay >= start && minute + MinutesPerDay < end)
                return new OpenState(true, yesterday.AddMinutes(end));
        }

        return new OpenState(false, null);
    }

    // OSM semantics: a later rule that matches the day replaces what earlier rules said about it
    private IReadOnlyList<(int Start, int End)> RangesOn(DateTime date)
    {
        var dayIndex = ((int)date.DayOfWeek + DaysPerWeek - 1) % DaysPerWeek;
        IReadOnlyList<(int Start, int End)> ranges = [];
        foreach (var rule in _rules.Where(rule => rule.Months[date.Month - 1] && rule.Days[dayIndex]))
            ranges = rule.Ranges;
        return ranges;
    }

    private static Rule? ParseRule(string part)
    {
        var match = RulePattern().Match(part);
        if (!match.Success)
            return null;

        var months = match.Groups["months"].Success ? ParseSelector(match.Groups["months"].Value, MonthNames) : AllTrue(MonthsPerYear);
        var days = match.Groups["days"].Success ? ParseSelector(match.Groups["days"].Value, DayNames) : AllTrue(DaysPerWeek);
        var ranges = ParseTimes(match.Groups["times"].Value);
        return months is null || days is null || ranges is null ? null : new Rule(months, days, ranges);
    }

    // "Mo-Fr,Su" or "May-Oct"; ranges may wrap ("Fr-Mo", "Nov-Feb")
    private static bool[]? ParseSelector(string selector, string[] names)
    {
        var selected = new bool[names.Length];
        foreach (var item in selector.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var bounds = item.Split('-', StringSplitOptions.TrimEntries);
            var first = Array.IndexOf(names, bounds[0]);
            var last = bounds.Length == 2 ? Array.IndexOf(names, bounds[1]) : first;
            if (first < 0 || last < 0 || bounds.Length > 2)
                return null;
            for (var index = first; ; index = (index + 1) % names.Length)
            {
                selected[index] = true;
                if (index == last) break;
            }
        }
        return selected;
    }

    private static IReadOnlyList<(int Start, int End)>? ParseTimes(string times)
    {
        if (times is "off" or "closed")
            return [];
        if (times == "24/7")
            return [(0, MinutesPerDay)];

        var ranges = new List<(int Start, int End)>();
        foreach (var span in times.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var match = TimeSpanPattern().Match(span);
            if (!match.Success)
                return null;
            var start = ToMinutes(match.Groups["start"].Value);
            var end = ToMinutes(match.Groups["end"].Value);
            if (start is null || end is null || start >= MinutesPerDay)
                return null;
            ranges.Add((start.Value, end.Value <= start.Value ? end.Value + MinutesPerDay : end.Value));
        }
        return ranges;
    }

    private static int? ToMinutes(string clock)
    {
        var hours = int.Parse(clock[..2]);
        var minutes = int.Parse(clock[3..]);
        if (minutes >= 60 || hours > 24 || (hours == 24 && minutes > 0))
            return null;
        return hours * 60 + minutes;
    }

    private static bool[] AllTrue(int length) => Enumerable.Repeat(true, length).ToArray();

    // Rules are split on ";" and also on ", " when a time ends one rule and a day starts the next ("Mo-Sa 18:00-01:00, Su 14:30-24:00")
    [GeneratedRegex(@";|(?<=\d)\s*,\s*(?=(?:Mo|Tu|We|Th|Fr|Sa|Su|PH|SH)\b)")]
    private static partial Regex RuleSeparator();

    [GeneratedRegex(@"\b(?:PH|SH)\b")]
    private static partial Regex HolidaySelector();

    [GeneratedRegex(@"^(?:(?<months>(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)(?:\s*-\s*[A-Z][a-z]{2})?(?:\s*,\s*[A-Z][a-z]{2}(?:\s*-\s*[A-Z][a-z]{2})?)*)\s*:?\s+)?(?:(?<days>(?:Mo|Tu|We|Th|Fr|Sa|Su)(?:\s*-\s*[A-Z][a-z])?(?:\s*,\s*(?:Mo|Tu|We|Th|Fr|Sa|Su)(?:\s*-\s*[A-Z][a-z])?)*)\s+)?(?<times>off|closed|24/7|\d{2}:\d{2}\s*-\s*\d{2}:\d{2}(?:\s*,\s*\d{2}:\d{2}\s*-\s*\d{2}:\d{2})*)$")]
    private static partial Regex RulePattern();

    [GeneratedRegex(@"^(?<start>\d{2}:\d{2})\s*-\s*(?<end>\d{2}:\d{2})$")]
    private static partial Regex TimeSpanPattern();
}
