using Microsoft.EntityFrameworkCore;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;
using BearNecessities.Infrastructure.Persistence;

namespace BearNecessities.Infrastructure.Repositories;

public class PlaceRepository : BaseRepository<Place, PlaceId>, IPlaceRepository
{
    public PlaceRepository(BearNecessitiesDbContext context) : base(context) { }

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
        var inViewport = WhereInAnyCategory(
            Context.Places.Where(p => p.Status == ListingStatus.Live
                && p.Latitude >= viewport.SouthWest.Latitude
                && p.Latitude <= viewport.NorthEast.Latitude
                && p.Longitude >= viewport.SouthWest.Longitude
                && p.Longitude <= viewport.NorthEast.Longitude),
            categoryIds?.Select(c => c.Value).ToList());

        // First page (the map's case): count and page in SQL instead of loading the whole viewport
        if (!cursor.HasValue)
        {
            var totalCount = await inViewport.CountAsync(cancellationToken);
            var firstPage = await inViewport
                .Include(p => p.Categories)
                .OrderBy(p => p.CreatedAt)
                .ThenBy(p => p.Id)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            return (firstPage, totalCount);
        }

        var allItems = await inViewport.Include(p => p.Categories).ToListAsync(cancellationToken);

        var ordered = allItems
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

    // Filters through the PlaceCategories join table: EF Core 8 can't translate c.Id.Value on the
    // value-converted key inside Categories.Any(), but it can translate a join-table subquery.
    private IQueryable<Place> WhereInAnyCategory(IQueryable<Place> query, IReadOnlyCollection<Guid>? categoryIds)
    {
        if (categoryIds is not { Count: > 0 })
            return query;

        var categoryKeys = categoryIds.Select(id => new CategoryId(id)).ToList();
        var placeIdsInCategories = Context.Set<Dictionary<string, object>>("PlaceCategories")
            .Where(link => categoryKeys.Contains(EF.Property<CategoryId>(link, "CategoryId")))
            .Select(link => EF.Property<PlaceId>(link, "PlaceId"));
        return query.Where(p => placeIdsInCategories.Contains(p.Id));
    }

    public async Task<IReadOnlyList<PlaceDensityCell>> GetDensityAsync(
        GeoViewport viewport,
        IReadOnlyCollection<Guid>? categoryIds,
        decimal cellSizeDegrees,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Places.Where(p => p.Status == ListingStatus.Live
            && p.Latitude >= viewport.SouthWest.Latitude
            && p.Latitude <= viewport.NorthEast.Latitude
            && p.Longitude >= viewport.SouthWest.Longitude
            && p.Longitude <= viewport.NorthEast.Longitude);

        query = WhereInAnyCategory(query, categoryIds);

        // GROUP BY runs in SQL Server: one row per occupied cell instead of one per place
        return await query
            .GroupBy(p => new
            {
                Row = Math.Floor(p.Latitude / cellSizeDegrees),
                Column = Math.Floor(p.Longitude / cellSizeDegrees)
            })
            .Select(cell => new PlaceDensityCell(
                cell.Average(p => p.Latitude),
                cell.Average(p => p.Longitude),
                cell.Count(),
                cell.Min(p => p.Latitude),
                cell.Min(p => p.Longitude),
                cell.Max(p => p.Latitude),
                cell.Max(p => p.Longitude)))
            .ToListAsync(cancellationToken);
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

    public async Task<List<Place>> GetByIdsWithRelationsAsync(
        IEnumerable<PlaceId> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await Context.Places
            .Include(p => p.Categories)
            .Include(p => p.ExternalLinks)
            .Where(p => idList.Contains(p.Id))
            .ToListAsync(cancellationToken);
    }
}
