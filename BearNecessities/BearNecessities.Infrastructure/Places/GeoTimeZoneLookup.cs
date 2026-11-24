using BearNecessities.Application.Interfaces;
using GeoTimeZone;

namespace BearNecessities.Infrastructure.Places;

/// <summary>Offline time-zone lookup from coordinates (GeoTimeZone ships the zone boundaries with the package).</summary>
public sealed class GeoTimeZoneLookup : ITimeZoneLookup
{
    public string FindTimeZone(decimal latitude, decimal longitude) =>
        TimeZoneLookup.GetTimeZone((double)latitude, (double)longitude).Result;

    public DateTime ToLocalTime(DateTime utcTime, decimal latitude, decimal longitude)
    {
        var utc = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
        return TimeZoneInfo.TryFindSystemTimeZoneById(FindTimeZone(latitude, longitude), out var zone)
            ? TimeZoneInfo.ConvertTimeFromUtc(utc, zone)
            : utc;
    }
}
