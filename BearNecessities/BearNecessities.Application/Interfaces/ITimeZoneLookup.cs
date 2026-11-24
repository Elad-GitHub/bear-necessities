namespace BearNecessities.Application.Interfaces;

/// <summary>Finds the IANA time zone at a point, so local times (opening hours, events) are read correctly.</summary>
public interface ITimeZoneLookup
{
    string FindTimeZone(decimal latitude, decimal longitude);

    /// <summary>The local time at the point; falls back to UTC when the zone is unknown to this machine.</summary>
    DateTime ToLocalTime(DateTime utcTime, decimal latitude, decimal longitude);
}
