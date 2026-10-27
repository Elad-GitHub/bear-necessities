using Microsoft.EntityFrameworkCore;
using Nomadica.Domain.Entities;
using Nomadica.Domain.Enums;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;
using Nomadica.Infrastructure.Persistence;

namespace Nomadica.Infrastructure.Repositories;

public class PlaceRepository : BaseRepository<Place, PlaceId>, IPlaceRepository
{
    public PlaceRepository(NomadiaDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Place>> FindByViewportAsync(
        GeoViewport viewport,
        IEnumerable<CategoryId>? categoryIds,
        CancellationToken cancellationToken = default)
    {
        var items = await Context.Places
            .Include(p => p.Categories)
            .Where(p => p.Status == ListingStatus.Live
                && p.Latitude >= viewport.SouthWest.Latitude
                && p.Latitude <= viewport.NorthEast.Latitude
                && p.Longitude >= viewport.SouthWest.Longitude
                && p.Longitude <= viewport.NorthEast.Longitude)
            .ToListAsync(cancellationToken);

        // Category filter runs in memory: c.Id.Value (value-object member) cannot be
        // translated to SQL by EF Core 8 inside an Any subquery.
        var guids = categoryIds?.Select(c => c.Value).ToHashSet();
        return guids is { Count: > 0 }
            ? items.Where(p => p.Categories.Any(c => guids.Contains(c.Id.Value))).ToList()
            : items;
    }

    public async Task<List<Place>> SearchByTextAsync(
        string query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var pattern = $"%{query.Trim()}%";

        // Match by name or city only — whole countries are handled as map destinations (geocoder)
        return await Context.Places
            .Include(p => p.Categories)
            .Where(p => p.Status == ListingStatus.Live
                && (EF.Functions.Like(p.Name, pattern)
                    || EF.Functions.Like(p.City!, pattern)))
            .OrderBy(p => p.Name)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Place>> FindByCountryAsync(
        string country,
        CancellationToken cancellationToken = default)
    {
        var pattern = $"%{country.Trim()}%";

        return await Context.Places
            .Include(p => p.Categories)
            .Where(p => p.Status == ListingStatus.Live && EF.Functions.Like(p.Country!, pattern))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Place>> GetByOwnerAsync(
        UserId ownerId,
        CancellationToken cancellationToken = default)
        => await Context.Places
            .Include(p => p.Categories)
            .Where(p => p.OwnerId == ownerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Place>> FindNearbyAsync(
        Coordinates center,
        double radiusKm,
        CancellationToken cancellationToken = default)
    {
        var deltaLat = (decimal)(radiusKm / 111.0);
        var latRad = (double)center.Latitude * Math.PI / 180.0;
        var deltaLon = (decimal)(radiusKm / (111.0 * Math.Cos(latRad)));

        return await Context.Places
            .Include(p => p.Categories)
            .Where(p => p.Status == ListingStatus.Live
                && p.Latitude >= center.Latitude - deltaLat
                && p.Latitude <= center.Latitude + deltaLat
                && p.Longitude >= center.Longitude - deltaLon
                && p.Longitude <= center.Longitude + deltaLon)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Place> Items, int TotalCount)> FindByViewportPagedAsync(
        GeoViewport viewport,
        IEnumerable<CategoryId>? categoryIds,
        Guid? cursor,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var allItems = await Context.Places
            .Include(p => p.Categories)
            .Where(p => p.Status == ListingStatus.Live
                && p.Latitude >= viewport.SouthWest.Latitude
                && p.Latitude <= viewport.NorthEast.Latitude
                && p.Longitude >= viewport.SouthWest.Longitude
                && p.Longitude <= viewport.NorthEast.Longitude)
            .ToListAsync(cancellationToken);

        // Category filter runs in memory: c.Id.Value (value-object member) cannot be
        // translated to SQL by EF Core 8 inside an Any subquery.
        var categoryGuids = categoryIds?.Select(c => c.Value).ToHashSet();
        var filtered = categoryGuids is { Count: > 0 }
            ? allItems.Where(p => p.Categories.Any(c => categoryGuids.Contains(c.Id.Value))).ToList()
            : allItems;

        var ordered = filtered
            .OrderBy(p => p.CreatedAt)
            .ThenBy(p => p.Id.Value)
            .ToList();

        IEnumerable<Place> page = ordered;
        if (cursor.HasValue)
        {
            var cursorIndex = ordered.FindIndex(p => p.Id.Value == cursor.Value);
            if (cursorIndex >= 0)
                page = ordered.Skip(cursorIndex + 1);
        }

        return (page.Take(pageSize).ToList(), ordered.Count);
    }

    public async Task<Place?> GetByIdWithRelationsAsync(
        PlaceId id,
        CancellationToken cancellationToken = default)
    {
        return await Context.Places
            .Include(p => p.Categories)
            .Include(p => p.ExternalLinks)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<List<Place>> GetPendingAsync(
        int pageSize,
        Guid? cursor,
        CancellationToken cancellationToken = default)
    {
        var all = await Context.Places
            .Include(p => p.Categories)
            .Where(p => p.Status == ListingStatus.PendingReview)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        if (cursor.HasValue)
        {
            var idx = all.FindIndex(p => p.Id.Value == cursor.Value);
            if (idx >= 0)
                all = all.Skip(idx + 1).ToList();
        }

        return all.Take(pageSize).ToList();
    }

    public async Task ReplaceExternalLinksAsync(
        PlaceId placeId,
        IEnumerable<PlaceExternalLink> newLinks,
        CancellationToken cancellationToken = default)
    {
        var existing = await Context.PlaceExternalLinks
            .Where(l => l.PlaceId == placeId)
            .ToListAsync(cancellationToken);

        Context.PlaceExternalLinks.RemoveRange(existing);
        await Context.PlaceExternalLinks.AddRangeAsync(newLinks, cancellationToken);
    }

    public async Task<List<Place>> GetByIdsAsync(
        IEnumerable<PlaceId> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await Context.Places
            .Include(p => p.Categories)
            .Where(p => idList.Contains(p.Id))
            .ToListAsync(cancellationToken);
    }
}
