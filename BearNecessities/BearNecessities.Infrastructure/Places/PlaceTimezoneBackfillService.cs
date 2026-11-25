using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BearNecessities.Application.Interfaces;
using BearNecessities.Application.Services.Places;
using BearNecessities.Infrastructure.Persistence;

namespace BearNecessities.Infrastructure.Places;

public sealed record TimezoneBackfillReport(int Checked, int Updated, int LeftAsUtc, IReadOnlyDictionary<string, int> UpdatedByCountry)
{
    public override string ToString() =>
        $"checked={Checked}, updated={Updated}, leftAsUtc={LeftAsUtc}; " +
        string.Join(", ", UpdatedByCountry.OrderByDescending(pair => pair.Value).Select(pair => $"{pair.Key}={pair.Value}"));
}

/// <summary>Gives places imported before zone lookup (stored as UTC) the real IANA zone at their coordinates.</summary>
public sealed class PlaceTimezoneBackfillService
{
    private readonly BearNecessitiesDbContext _db;
    private readonly ITimeZoneLookup _timeZoneLookup;
    private readonly ILogger<PlaceTimezoneBackfillService> _logger;

    public PlaceTimezoneBackfillService(BearNecessitiesDbContext db, ITimeZoneLookup timeZoneLookup, ILogger<PlaceTimezoneBackfillService> logger)
    {
        _db = db;
        _timeZoneLookup = timeZoneLookup;
        _logger = logger;
    }

    public async Task<TimezoneBackfillReport> BackfillAsync(CancellationToken cancellationToken = default)
    {
        var placeholders = PlaceTimezoneCorrection.PlaceholderZones;
        var places = await _db.Places
            .Where(place => placeholders.Contains(place.Timezone))
            .ToListAsync(cancellationToken);

        var updatedByCountry = new Dictionary<string, int>();
        var leftAsUtc = 0;
        foreach (var place in places)
        {
            var zone = PlaceTimezoneCorrection.FindRealZone(place.Latitude, place.Longitude, _timeZoneLookup);
            if (zone is null)
            {
                leftAsUtc++;
                _logger.LogInformation("No land time zone for {Place} ({Latitude}, {Longitude}); keeping UTC", place.Name, place.Latitude, place.Longitude);
                continue;
            }
            place.SetTimezone(zone);
            var country = place.CountryCode ?? "??";
            updatedByCountry[country] = updatedByCountry.GetValueOrDefault(country) + 1;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new TimezoneBackfillReport(places.Count, places.Count - leftAsUtc, leftAsUtc, updatedByCountry);
    }
}
