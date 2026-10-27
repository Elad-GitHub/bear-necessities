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
        "nwr[\"amenity\"=\"marketplace\"](area.a);"
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
        var query = BuildQuery(isoCountryCode, maxResults);
        _logger.LogInformation("Querying Overpass for {Iso}...", isoCountryCode);

        var json = await FetchWithFailoverAsync(query, isoCountryCode, cancellationToken);
        if (json is null)
            return new ImportReport(0, 0, 0, [$"Overpass unavailable for {isoCountryCode} after retries (rate-limited/timeout)."]);

        var places = ParseElements(json, isoCountryCode, countryName);
        _logger.LogInformation("Overpass returned {Count} usable POIs for {Iso}", places.Count, isoCountryCode);
        return await _importer.ImportAsync(places, autoApprove, cancellationToken);
    }

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

    private static string BuildQuery(string iso, int maxResults)
    {
        var builder = new StringBuilder();
        builder.AppendLine("[out:json][timeout:120];");
        builder.AppendLine($"area[\"ISO3166-1\"=\"{iso.ToUpperInvariant()}\"][admin_level=2]->.a;");
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
