using BearNecessities.Application.Interfaces;

namespace BearNecessities.Application.Services.Places;

/// <summary>Finds the real zone for places saved with a placeholder (UTC or empty) before imports looked zones up.</summary>
public static class PlaceTimezoneCorrection
{
    public static readonly string[] PlaceholderZones = ["UTC", "Etc/UTC", ""];

    public static bool NeedsCorrection(string? timezone) =>
        string.IsNullOrWhiteSpace(timezone) || PlaceholderZones.Contains(timezone, StringComparer.OrdinalIgnoreCase);

    /// <summary>The zone to store, or null when the point only has a sea offset zone (Etc/GMT±n) and UTC may as well stay.</summary>
    public static string? FindRealZone(decimal latitude, decimal longitude, ITimeZoneLookup lookup)
    {
        var zone = lookup.FindTimeZone(latitude, longitude);
        if (string.IsNullOrWhiteSpace(zone) || zone.StartsWith("Etc/", StringComparison.OrdinalIgnoreCase)) return null;
        return zone;
    }
}
