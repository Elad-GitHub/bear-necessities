using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using BearNecessities.Application.Imports;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Enums;

namespace BearNecessities.Infrastructure.Imports;

/// Pulls conscious-community POIs from OpenStreetMap via the Overpass API and
/// funnels them through the shared import pipeline.
public sealed class OsmImportService
{
    // Several public Overpass mirrors — we fail over between them on rate-limit/timeout
    private static readonly string[] OverpassEndpoints =
    [
        "https://overpass-api.de/api/interpreter",
        "https://overpass.kumi.systems/api/interpreter",
        "https://overpass.private.coffee/api/interpreter",
        "https://maps.mail.ru/osm/tools/overpass/api/interpreter"
    ];

    // Telling words in a name ("Dhamma Giri", "Lisbon Qigong School"). Matched only on objects that already
    // carry a place-type key: a bare name regex scans every named object in the country and times out.
    private const string PracticeNamePattern =
        "^Dhamma |vipassana|meditation cent|retreat|eco ?village|ecovila|ecoaldea|ecstatic dance|permacult|ayurved|reiki|kirtan|temazcal|sound healing|cacao ceremony|qi ?gong|tai ?chi|breathwork|acro ?yoga|contact improv|circus school|kambo|shaman|wwoof|co-?living|ice bath|wim hof|tuina|tui na|drum circle|sound bath|macram|human design";

    private static readonly string[] PlaceTypeKeys =
        ["amenity", "leisure", "tourism", "shop", "healthcare", "club", "sport", "office", "craft", "building", "landuse"];

    private static string PracticeNameFilter() =>
        "(" + string.Concat(PlaceTypeKeys.Select(key =>
            $"nwr[\"{key}\"][\"name\"~\"{PracticeNamePattern}\",i](area.a);")) + ");";

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
        "nwr[\"leisure\"=\"garden\"][\"garden:type\"=\"community\"](area.a);",
        // Healing, retreats and spiritual places, which OSM tags directly or names plainly
        "nwr[\"healthcare\"=\"alternative\"](area.a);",
        "nwr[\"healthcare:speciality\"~\"ayurveda|acupuncture|reiki\"](area.a);",
        "nwr[\"shop\"=\"massage\"](area.a);",
        "nwr[\"shop\"~\"^(herbalist|esoteric)$\"](area.a);",
        "nwr[\"amenity\"=\"public_bath\"][\"bath:type\"~\"hot_spring|onsen\"](area.a);",
        // Community spaces and practices OSM tags as their own kind
        "nwr[\"amenity\"=\"coworking_space\"](area.a);",
        "nwr[\"leisure\"=\"hackerspace\"](area.a);",
        "nwr[\"sport\"~\"^(tai_chi|qigong)$\"](area.a);",
        "nwr[\"craft\"=\"pottery\"](area.a);",
        "nwr[\"amenity\"=\"music_venue\"](area.a);"
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
        int maxResultsPerKind = 300,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(CountryAreaSelector(isoCountryCode), maxResultsPerKind);
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
        int maxResultsPerKind = 300,
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
                BuildQuery(RegionAreaSelector(region.RelationId), maxResultsPerKind), label, cancellationToken);

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

    private sealed record AdminRegion(long RelationId, string Name, int AdminLevel);

    // Region tiers differ by country (states, provinces, regions); level 4 is the most common first tier
    private static readonly int[] RegionAdminLevels = [4, 3, 5];
    private const int MinimumUsefulRegionCount = 2;
    private const long OverpassAreaIdOffset = 3_600_000_000;
    private static readonly TimeSpan RegionPause = TimeSpan.FromSeconds(5);

    // Looks regions up by their ISO 3166-2 code ("IN-KA", "GR-M"...): a tag-index query that returns every
    // level at once in seconds, where "relations inside the country's area" timed out for India-sized countries
    private async Task<List<AdminRegion>> FindRegionsAsync(string iso, CancellationToken cancellationToken)
    {
        if (!IsoCountryCodePattern.IsMatch(iso))
            throw new ArgumentException($"'{iso}' is not a two-letter ISO country code.", nameof(iso));

        var query = $"""
            [out:json][timeout:90];
            rel["ISO3166-2"~"^{iso}-"]["boundary"="administrative"];
            out tags;
            """;
        var json = await FetchWithFailoverAsync(query, $"{iso} regions", cancellationToken);
        if (json is null)
            return [];

        var regionsByLevel = ParseRegions(json, iso).ToLookup(region => region.AdminLevel);
        foreach (var adminLevel in RegionAdminLevels)
        {
            var regions = regionsByLevel[adminLevel].ToList();
            _logger.LogInformation("{Iso}: {Count} regions at admin level {Level}", iso, regions.Count, adminLevel);
            if (regions.Count >= MinimumUsefulRegionCount)
                return regions;
        }

        return [];
    }

    private static readonly System.Text.RegularExpressions.Regex IsoCountryCodePattern = new("^[A-Z]{2}$");

    // The regex match is a prefix check on a tag value; re-check it here so a stray match can never import another country
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

            if (!int.TryParse(GetTag(tags, "admin_level"), out var adminLevel))
                continue;

            var name = GetTag(tags, "name:en") ?? GetTag(tags, "name") ?? subdivisionCode;
            regions.Add(new AdminRegion(idProperty.GetInt64(), name, adminLevel));
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

    // Each kind gets its own cap, so thousands of markets can't crowd rarer kinds (reiki, temazcal) out of the result
    private static string BuildQuery(string areaSelector, int maxResultsPerKind)
    {
        var builder = new StringBuilder();
        builder.AppendLine("[out:json][timeout:180];");
        builder.AppendLine(areaSelector);
        foreach (var filter in CandidateFilters.Append(PracticeNameFilter()))
        {
            builder.AppendLine(filter);
            builder.AppendLine($"out center {maxResultsPerKind};");
        }
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

            var name = GetTag(tags, "name:en") ?? GetTag(tags, "name");
            if (string.IsNullOrWhiteSpace(name)) continue;

            var tagDictionary = ToTagDictionary(tags);
            var slugs = OsmCategoryRules.Classify(tagDictionary);
            if (slugs.Count == 0) continue;

            if (!TryGetCoordinates(element, out var latitude, out var longitude)) continue;

            var elementType = GetString(element, "type");
            var elementId = GetString(element, "id");

            results.Add(new ImportPlace(
                Name: name!,
                Latitude: latitude,
                Longitude: longitude,
                CategorySlugs: slugs.ToList(),
                Source: ListingSourceType.Osm,
                ExternalSourceId: $"osm:{elementType}/{elementId}",
                // Most OSM places have no description; a summary built from their tags fills the hover card
                Description: GetTag(tags, "description") ?? OsmPlaceSummary.Build(tagDictionary, slugs[0]),
                Address: BuildAddress(tags),
                City: GetTag(tags, "addr:city"),
                Country: countryName ?? iso,
                CountryCode: iso.ToUpperInvariant(),
                Website: GetTag(tags, "website") ?? GetTag(tags, "contact:website"),
                IsPermanent: true));
        }

        return results;
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

    private static Dictionary<string, string> ToTagDictionary(JsonElement tags) =>
        tags.EnumerateObject()
            .Where(tag => tag.Value.ValueKind == JsonValueKind.String)
            .ToDictionary(tag => tag.Name, tag => tag.Value.GetString()!, StringComparer.OrdinalIgnoreCase);

    private static string? GetTag(JsonElement tags, string key)
        => tags.TryGetProperty(key, out var value) ? value.GetString() : null;

    private static string GetString(JsonElement element, string key)
        => element.TryGetProperty(key, out var value)
            ? (value.ValueKind == JsonValueKind.Number ? value.GetInt64().ToString() : value.GetString() ?? string.Empty)
            : string.Empty;
}
