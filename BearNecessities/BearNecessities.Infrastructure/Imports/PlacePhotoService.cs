using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BearNecessities.Application.Imports;
using BearNecessities.Domain.ValueObjects;
using BearNecessities.Infrastructure.Persistence;

namespace BearNecessities.Infrastructure.Imports;

/// <summary>
/// Gives places a photo from Wikimedia Commons: geotagged files taken near the place whose name matches it,
/// under a licence that allows reuse with credit. Places that already have a photo are left alone.
/// </summary>
public sealed class PlacePhotoService
{
    private const string CommonsApi = "https://commons.wikimedia.org/w/api.php";
    private const int SearchRadiusMeters = 150;
    private const int CandidatesPerPlace = 30;
    private const int PhotoWidthPixels = 1280;
    // Wikimedia asks API clients to keep the load modest; a few requests in flight is plenty
    private const int ParallelRequests = 3;
    private const int ChunkSize = 200;
    private static readonly TimeSpan RateLimitPause = TimeSpan.FromSeconds(30);

    private readonly HttpClient _http;
    private readonly BearNecessitiesDbContext _db;
    private readonly ILogger<PlacePhotoService> _logger;

    public PlacePhotoService(HttpClient http, BearNecessitiesDbContext db, ILogger<PlacePhotoService> logger)
    {
        _http = http;
        _db = db;
        _logger = logger;
    }

    public async Task<(int Checked, int Found)> FindMissingPhotosAsync(
        string? countryCode, int? limit, CancellationToken cancellationToken = default)
    {
        var query = _db.Places.Where(p => p.PhotoUrl == null);
        if (!string.IsNullOrWhiteSpace(countryCode))
            query = query.Where(p => p.CountryCode == countryCode.ToUpperInvariant());
        var places = await query
            .OrderBy(p => p.CreatedAt)
            .Select(p => new PlaceToSearch(p.Id, p.Name, p.City, p.Latitude, p.Longitude))
            .Take(limit ?? int.MaxValue)
            .ToListAsync(cancellationToken);
        _logger.LogInformation("Looking for photos for {Count} places", places.Count);

        var found = 0;
        foreach (var chunk in places.Chunk(ChunkSize))
        {
            var photos = new Dictionary<PlaceId, PlacePhoto>();
            await Parallel.ForEachAsync(chunk,
                new ParallelOptions { MaxDegreeOfParallelism = ParallelRequests, CancellationToken = cancellationToken },
                async (place, token) =>
                {
                    var photo = await FindPhotoAsync(place, token);
                    if (photo is null) return;
                    lock (photos) photos[place.Id] = photo;
                });

            found += await SavePhotosAsync(photos, cancellationToken);
            _logger.LogInformation("Photos: {Found} found so far", found);
        }

        return (places.Count, found);
    }

    private async Task<int> SavePhotosAsync(Dictionary<PlaceId, PlacePhoto> photos, CancellationToken cancellationToken)
    {
        if (photos.Count == 0) return 0;

        var ids = photos.Keys.ToList();
        var places = await _db.Places.Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);
        foreach (var place in places)
        {
            var photo = photos[place.Id];
            place.SetPhoto(photo.Url, photo.Credit, photo.CreditUrl);
        }
        await _db.SaveChangesAsync(cancellationToken);
        return places.Count;
    }

    private async Task<PlacePhoto?> FindPhotoAsync(PlaceToSearch place, CancellationToken cancellationToken)
    {
        try
        {
            var nearby = await SearchNearbyAsync(place.Latitude, place.Longitude, cancellationToken);
            var photo = CommonsPhotoMatching.PickPhoto(place.Name, place.City, nearby);
            if (photo is not null || string.IsNullOrWhiteSpace(place.City)) return photo;

            // Many good photos carry no location, so also search by name and city
            var named = await SearchByNameAsync($"{place.Name} {place.City}", cancellationToken);
            return CommonsPhotoMatching.PickPhotoFromSearch(place.Name, place.City, named);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Photo search failed for {Name}: {Message}", place.Name, ex.Message);
            return null;
        }
    }

    // Commons files within the radius, nearest first
    private Task<List<CommonsPhotoCandidate>> SearchNearbyAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken)
    {
        var coordinates = string.Create(CultureInfo.InvariantCulture, $"{latitude}|{longitude}");
        return QueryCandidatesAsync(
            $"generator=geosearch&ggscoord={Uri.EscapeDataString(coordinates)}&ggsradius={SearchRadiusMeters}"
            + $"&ggslimit={CandidatesPerPlace}&ggsnamespace=6", cancellationToken);
    }

    // Commons files whose title or description matches the text, best match first
    private Task<List<CommonsPhotoCandidate>> SearchByNameAsync(string text, CancellationToken cancellationToken) =>
        QueryCandidatesAsync(
            $"generator=search&gsrsearch={Uri.EscapeDataString(text)}&gsrlimit={CandidatesPerPlace}&gsrnamespace=6",
            cancellationToken);

    // One request returns the files with their image URL, author and licence
    private async Task<List<CommonsPhotoCandidate>> QueryCandidatesAsync(string generator, CancellationToken cancellationToken)
    {
        var url = $"{CommonsApi}?action=query&format=json&formatversion=2&maxlag=5&{generator}"
            + $"&prop=imageinfo&iiprop=url|extmetadata&iiurlwidth={PhotoWidthPixels}&iiextmetadatafilter=Artist|LicenseShortName";

        using var response = await _http.GetAsync(url, cancellationToken);
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            await Task.Delay(RateLimitPause, cancellationToken);
            return [];
        }
        response.EnsureSuccessStatusCode();

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("query", out var queryResult)
            || !queryResult.TryGetProperty("pages", out var pages))
            return [];

        // Pages come back in arbitrary order; "index" is the search rank
        return pages.EnumerateArray()
            .Where(page => page.TryGetProperty("imageinfo", out _))
            .OrderBy(page => page.TryGetProperty("index", out var index) ? index.GetInt32() : int.MaxValue)
            .Select(ToCandidate)
            .ToList();
    }

    private static CommonsPhotoCandidate ToCandidate(JsonElement page)
    {
        var info = page.GetProperty("imageinfo")[0];
        var metadata = info.TryGetProperty("extmetadata", out var extmetadata) ? extmetadata : default;
        return new CommonsPhotoCandidate(
            FileTitle: page.GetProperty("title").GetString() ?? string.Empty,
            ImageUrl: StripTracking(info.TryGetProperty("thumburl", out var thumb) ? thumb.GetString() : info.GetProperty("url").GetString()),
            PageUrl: info.GetProperty("descriptionurl").GetString() ?? string.Empty,
            Artist: MetadataValue(metadata, "Artist"),
            License: MetadataValue(metadata, "LicenseShortName"));
    }

    private static string? MetadataValue(JsonElement metadata, string key) =>
        metadata.ValueKind == JsonValueKind.Object && metadata.TryGetProperty(key, out var field)
            && field.TryGetProperty("value", out var value) ? value.GetString() : null;

    // Commons appends "?utm_source=..." to image URLs; the bare URL is the stable one
    private static string StripTracking(string? url) => url is null ? string.Empty : url.Split('?')[0];

    private sealed record PlaceToSearch(PlaceId Id, string Name, string? City, decimal Latitude, decimal Longitude);
}
