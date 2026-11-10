using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nomadica.Application.Imports;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Enums;

namespace Nomadica.Infrastructure.Imports;

/// Pulls conscious-community POIs from OpenStreetMap via the Overpass API and
/// funnels them through the shared import pipeline.
public sealed class OsmImportService
{
    // Several public Overpass mirrors — we fail over between them on rate-limit/timeout
    private static readonly string[] OverpassEndpoints =
    [
        "https://overpass-api.de/api/interpreter",
        "https://overpass.kumi.systems/api/interpreter",
        "https://overpass.private.coffee/api/interpreter"
    ];

    // Overpass filters we union to find candidates within a country area.
    private static readonly string[] CandidateFilters =
    [
        "nwr[\"sport\"=\"yoga\"](area.a);",
        "nwr[\"cuisine\"~\"vegan|vegetarian\"](area.a);",
        "nwr[\"diet:vegan\"~\"yes|only\"][\"amenity\"=\"restaurant\"](area.a);",
        "nwr[\"cuisine\"~\"indian\"][\"amenity\"=\"restaurant\"](area.a);",
        "nwr[\"shop\"=\"health_food\"](area.a);",
        "nwr[\"tourism\"=\"camp_site\"](area.a);",
        "nwr[\"natural\"=\"hot_spring\"](area.a);",
        "nwr[\"amenity\"=\"marketplace\"](area.a);",
        "nwr[\"leisure\"=\"garden\"][\"garden:type\"=\"community\"](area.a);"
    ];

    private readonly HttpClient _http;
    private readonly IPlaceImportService _importer;
    private readonly ILogger<OsmImportService> _logger;

    public OsmImportService(HttpClient http, IPlaceImportService importer, ILogger<OsmImportService> logger)
    {
        _http = http;
        _importer = importer;
        _logger = logger;
    }

    public async Task<ImportReport> ImportCountryAsync(
        string isoCountryCode,
        string? countryName,
        bool autoApprove,
        int maxResults = 1500,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(CountryAreaSelector(isoCountryCode), maxResults);
        _logger.LogInformation("Querying Overpass for {Iso}...", isoCountryCode);

        var json = await FetchWithFailoverAsync(query, isoCountryCode, cancellationToken);
        if (json is null)
            return new ImportReport(0, 0, 0, [$"Overpass unavailable for {isoCountryCode} after retries (rate-limited/timeout)."]);

        var places = ParseElements(json, isoCountryCode, countryName);
        _logger.LogInformation("Overpass returned {Count} usable POIs for {Iso}", places.Count, isoCountryCode);
        return await _importer.ImportAsync(places, autoApprove, cancellationToken);
    }

    /// <summary>
    /// Imports a country one administrative region at a time. Big countries time out as a single Overpass
    /// query and also hit the per-query result cap, so splitting fixes both.
    /// </summary>
    public async Task<ImportReport> ImportCountryByRegionAsync(
        string isoCountryCode,
        string? countryName,
        bool autoApprove,
        int maxResultsPerRegion = 1500,
        CancellationToken cancellationToken = default)
    {
        var iso = isoCountryCode.ToUpperInvariant();
        var regions = await FindRegionsAsync(iso, cancellationToken);
        if (regions.Count == 0)
            return new ImportReport(0, 0, 0, [$"No administrative regions found for {iso}."]);

        _logger.LogInformation("Importing {Iso} across {Count} regions", iso, regions.Count);
        var reports = new List<ImportReport>();
        for (var index = 0; index < regions.Count; index++)
        {
            var region = regions[index];
            var label = $"{iso}/{region.Name}";
            var json = await FetchWithFailoverAsync(
                BuildQuery(RegionAreaSelector(region.RelationId), maxResultsPerRegion), label, cancellationToken);

            if (json is null)
            {
                reports.Add(new ImportReport(0, 0, 0, [$"{region.Name}: Overpass unavailable after retries."]));
                continue;
            }

            var report = await _importer.ImportAsync(ParseElements(json, iso, countryName), autoApprove, cancellationToken);
            reports.Add(report);
            _logger.LogInformation("[{Index}/{Total}] {Region}: {Report}", index + 1, regions.Count, region.Name, report);

            // Public Overpass mirrors rate-limit bursts; a short pause keeps us under the radar
            await Task.Delay(RegionPause, cancellationToken);
        }

        return ImportReport.Combine(reports);
    }

    private sealed record AdminRegion(long RelationId, string Name);

    // Region tiers differ by country (states, provinces, regions); level 4 is the most common first tier
    private static readonly int[] RegionAdminLevels = [4, 3, 5];
    private const int MinimumUsefulRegionCount = 2;
    private const long OverpassAreaIdOffset = 3_600_000_000;
    private static readonly TimeSpan RegionPause = TimeSpan.FromSeconds(5);

    private async Task<List<AdminRegion>> FindRegionsAsync(string iso, CancellationToken cancellationToken)
    {
        foreach (var adminLevel in RegionAdminLevels)
        {
            var query = $"""
                [out:json][timeout:90];
                {CountryAreaSelector(iso)}
                rel(area.a)["boundary"="administrative"]["admin_level"="{adminLevel}"];
                out tags;
                """;
            var json = await FetchWithFailoverAsync(query, $"{iso} regions (level {adminLevel})", cancellationToken);
            if (json is null)
                continue;

            var regions = ParseRegions(json, iso);
            _logger.LogInformation("{Iso}: {Count} regions at admin level {Level}", iso, regions.Count, adminLevel);
            if (regions.Count >= MinimumUsefulRegionCount)
                return regions;
        }

        return [];
    }

    // rel(area) also returns neighbouring regions that touch the border, so keep only this country's ISO 3166-2 codes
    private static List<AdminRegion> ParseRegions(string json, string iso)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("elements", out var elements))
            return [];

        var regions = new List<AdminRegion>();
        foreach (var element in elements.EnumerateArray())
        {
            if (!element.TryGetProperty("tags", out var tags) || !element.TryGetProperty("id", out var idProperty))
                continue;

            var subdivisionCode = GetTag(tags, "ISO3166-2");
            if (subdivisionCode is null || !subdivisionCode.StartsWith($"{iso}-", StringComparison.OrdinalIgnoreCase))
                continue;

            var name = GetTag(tags, "name:en") ?? GetTag(tags, "name") ?? subdivisionCode;
            regions.Add(new AdminRegion(idProperty.GetInt64(), name));
        }
        return regions;
    }

    private static string CountryAreaSelector(string iso) =>
        $"area[\"ISO3166-1\"=\"{iso.ToUpperInvariant()}\"][admin_level=2]->.a;";

    private static string RegionAreaSelector(long relationId) =>
        $"area(id:{OverpassAreaIdOffset + relationId})->.a;";

    // Tries each mirror with backoff; treats 429/504/timeout as retryable rather than fatal
    private async Task<string?> FetchWithFailoverAsync(string query, string iso, CancellationToken cancellationToken)
    {
        var body = "data=" + Uri.EscapeDataString(query);

        for (var attempt = 0; attempt < OverpassEndpoints.Length; attempt++)
        {
            var endpoint = OverpassEndpoints[attempt];
            try
            {
                using var content = new StringContent(body);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");

                using var response = await _http.PostAsync(endpoint, content, cancellationToken);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsStringAsync(cancellationToken);

                _logger.LogWarning("Overpass {Endpoint} for {Iso} returned {Status}; trying next mirror",
                    endpoint, iso, (int)response.StatusCode);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Overpass {Endpoint} for {Iso} failed; trying next mirror", endpoint, iso);
            }

            // Brief backoff before the next mirror so we don't hammer a struggling server
            if (attempt < OverpassEndpoints.Length - 1)
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
        }

        return null;
    }

    private static string BuildQuery(string areaSelector, int maxResults)
    {
        var builder = new StringBuilder();
        builder.AppendLine("[out:json][timeout:120];");
        builder.AppendLine(areaSelector);
        builder.AppendLine("(");
        foreach (var filter in CandidateFilters)
            builder.AppendLine("  " + filter);
        builder.AppendLine(");");
        builder.AppendLine($"out center {maxResults};");
        return builder.ToString();
    }

    private List<ImportPlace> ParseElements(string json, string iso, string? countryName)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("elements", out var elements))
            return [];

        var results = new List<ImportPlace>();

        foreach (var element in elements.EnumerateArray())
        {
            if (!element.TryGetProperty("tags", out var tags)) continue;

            var name = GetTag(tags, "name");
            if (string.IsNullOrWhiteSpace(name)) continue;

            var slug = SlugForTags(tags);
            if (slug is null) continue;

            if (!TryGetCoordinates(element, out var latitude, out var longitude)) continue;

            var elementType = GetString(element, "type");
            var elementId = GetString(element, "id");

            results.Add(new ImportPlace(
                Name: name!,
                Latitude: latitude,
                Longitude: longitude,
                CategorySlugs: [slug],
                Source: ListingSourceType.Osm,
                ExternalSourceId: $"osm:{elementType}/{elementId}",
                Description: GetTag(tags, "description"),
                Address: BuildAddress(tags),
                City: GetTag(tags, "addr:city"),
                Country: countryName ?? iso,
                CountryCode: iso.ToUpperInvariant(),
                Website: GetTag(tags, "website") ?? GetTag(tags, "contact:website"),
                IsPermanent: true));
        }

        return results;
    }

    private static string? SlugForTags(JsonElement tags)
    {
        var sport = GetTag(tags, "sport");
        if (string.Equals(sport, "yoga", StringComparison.OrdinalIgnoreCase)) return "yoga";

        var cuisine = GetTag(tags, "cuisine")?.ToLowerInvariant() ?? string.Empty;
        var dietVegan = GetTag(tags, "diet:vegan")?.ToLowerInvariant();
        if (cuisine.Contains("vegan") || cuisine.Contains("vegetarian") || dietVegan is "yes" or "only")
            return "vegan-restaurant";
        if (cuisine.Contains("indian")) return "indian-food";

        if (string.Equals(GetTag(tags, "shop"), "health_food", StringComparison.OrdinalIgnoreCase))
            return "health-food-cafe";
        if (string.Equals(GetTag(tags, "tourism"), "camp_site", StringComparison.OrdinalIgnoreCase))
            return "camping";
        if (string.Equals(GetTag(tags, "natural"), "hot_spring", StringComparison.OrdinalIgnoreCase))
            return "hot-springs";
        if (string.Equals(GetTag(tags, "amenity"), "marketplace", StringComparison.OrdinalIgnoreCase))
            return "farmers-market";
        if (string.Equals(GetTag(tags, "leisure"), "garden", StringComparison.OrdinalIgnoreCase)
            && string.Equals(GetTag(tags, "garden:type"), "community", StringComparison.OrdinalIgnoreCase))
            return "community-garden";

        return null;
    }

    private static bool TryGetCoordinates(JsonElement element, out decimal latitude, out decimal longitude)
    {
        latitude = 0;
        longitude = 0;

        if (element.TryGetProperty("lat", out var latProp) && element.TryGetProperty("lon", out var lonProp))
        {
            latitude = latProp.GetDecimal();
            longitude = lonProp.GetDecimal();
            return true;
        }

        if (element.TryGetProperty("center", out var center)
            && center.TryGetProperty("lat", out var clat)
            && center.TryGetProperty("lon", out var clon))
        {
            latitude = clat.GetDecimal();
            longitude = clon.GetDecimal();
            return true;
        }

        return false;
    }

    private static string? BuildAddress(JsonElement tags)
    {
        var street = GetTag(tags, "addr:street");
        var houseNumber = GetTag(tags, "addr:housenumber");
        if (string.IsNullOrWhiteSpace(street)) return null;
        return string.IsNullOrWhiteSpace(houseNumber) ? street : $"{street} {houseNumber}";
    }

    private static string? GetTag(JsonElement tags, string key)
        => tags.TryGetProperty(key, out var value) ? value.GetString() : null;

    private static string GetString(JsonElement element, string key)
        => element.TryGetProperty(key, out var value)
            ? (value.ValueKind == JsonValueKind.Number ? value.GetInt64().ToString() : value.GetString() ?? string.Empty)
            : string.Empty;
}
