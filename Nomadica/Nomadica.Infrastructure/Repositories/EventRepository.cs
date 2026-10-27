using Microsoft.EntityFrameworkCore;
using Nomadica.Domain.Entities;
using Nomadica.Domain.Enums;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;
using Nomadica.Infrastructure.Persistence;

namespace Nomadica.Infrastructure.Repositories;

public class EventRepository : BaseRepository<Event, EventId>, IEventRepository
{
    public EventRepository(NomadiaDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Event>> FindUpcomingByPlaceAsync(
        PlaceId placeId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await Context.Events
            .Where(e => e.Status == ListingStatus.Live
                && e.PlaceId == placeId
                && (e.StartAt == null || e.StartAt >= now))
            .OrderBy(e => e.StartAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Event>> FindByViewportAsync(
        GeoViewport viewport,
        IEnumerable<CategoryId>? categoryIds,
        CancellationToken cancellationToken = default)
    {
        var query = from e in Context.Events
                    join p in Context.Places on EF.Property<Guid?>(e, "PlaceId") equals p.Id.Value
                    where e.Status == ListingStatus.Live
                       && p.Latitude >= viewport.SouthWest.Latitude
                       && p.Latitude <= viewport.NorthEast.Latitude
                       && p.Longitude >= viewport.SouthWest.Longitude
                       && p.Longitude <= viewport.NorthEast.Longitude
                    select e;

        if (categoryIds is not null)
        {
            var guids = categoryIds.Select(c => c.Value).ToList();
            query = query.Where(e => e.Categories.Any(c => guids.Contains(c.Id.Value)));
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<Event>> FindUpcomingByPlacesAsync(
        IEnumerable<PlaceId> placeIds,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var idList = placeIds.ToList();
        if (idList.Count == 0) return [];

        return await Context.Events
            .Include(e => e.Categories)
            .Where(e => e.Status == ListingStatus.Live
                && e.PlaceId != null
                && idList.Contains(e.PlaceId!)
                && (e.StartAt == null || e.StartAt >= now))
            .OrderBy(e => e.StartAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Event>> GetByOwnerAsync(
        UserId ownerId,
        CancellationToken cancellationToken = default)
        => await Context.Events
            .Include(e => e.Categories)
            .Where(e => e.OwnerId == ownerId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<Event?> GetByIdWithRelationsAsync(EventId id, CancellationToken cancellationToken = default)
        => await Context.Events
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<List<Event>> GetPendingAsync(int pageSize, Guid? cursor, CancellationToken cancellationToken = default)
    {
        var all = await Context.Events
            .Include(e => e.Categories)
            .Where(e => e.Status == ListingStatus.PendingReview)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        if (cursor.HasValue)
        {
            var idx = all.FindIndex(e => e.Id.Value == cursor.Value);
            if (idx >= 0)
                all = all.Skip(idx + 1).ToList();
        }

        return all.Take(pageSize).ToList();
    }

    public async Task<(IReadOnlyList<EventViewportItem> Items, int TotalCount)> FindByViewportPagedAsync(
        GeoViewport viewport,
        IEnumerable<CategoryId>? categoryIds,
        DateTime? dateFrom,
        DateTime? dateTo,
        int pageSize,
        Guid? cursor,
        CancellationToken cancellationToken = default)
    {
        // Two-query approach: EF can't translate JOIN on value-object FK in a single query.
        // Query 1: find places in viewport → keyed by raw Guid for O(1) lookup.
        var placesInViewport = await Context.Places
            .Where(p => p.Status == ListingStatus.Live
                && p.Latitude >= viewport.SouthWest.Latitude
                && p.Latitude <= viewport.NorthEast.Latitude
                && p.Longitude >= viewport.SouthWest.Longitude
                && p.Longitude <= viewport.NorthEast.Longitude)
            .Select(p => new { IdGuid = p.Id.Value, p.Latitude, p.Longitude })
            .ToListAsync(cancellationToken);

        if (placesInViewport.Count == 0)
            return (new List<EventViewportItem>(), 0);

        var placeMap = placesInViewport.ToDictionary(p => p.IdGuid);
        var placeIdValueObjects = placeMap.Keys.Select(g => new PlaceId(g)).ToList();

        // Query 2: events that belong to those places.
        var eventQuery = Context.Events
            .Include(e => e.Categories)
            .Where(e => e.Status == ListingStatus.Live
                && e.PlaceId != null
                && placeIdValueObjects.Contains(e.PlaceId!)
                && (dateFrom == null || e.StartAt >= dateFrom)
                && (dateTo == null || e.StartAt <= dateTo));

        var events = await eventQuery.ToListAsync(cancellationToken);

        var categoryGuids = categoryIds?.Select(c => c.Value).ToHashSet();
        var filtered = categoryGuids is { Count: > 0 }
            ? events.Where(e => e.Categories.Any(c => categoryGuids.Contains(c.Id.Value))).ToList()
            : events;

        var ordered = filtered
            .OrderBy(e => e.StartAt ?? DateTime.MaxValue)
            .ThenBy(e => e.Id.Value)
            .ToList();

        var startIndex = 0;
        if (cursor.HasValue)
        {
            var idx = ordered.FindIndex(e => e.Id.Value == cursor.Value);
            if (idx >= 0)
                startIndex = idx + 1;
        }

        var page = ordered.Skip(startIndex).Take(pageSize).ToList();
        var items = page.Select(e =>
        {
            if (e.PlaceId is null || !placeMap.ContainsKey(e.PlaceId.Value))
                return new EventViewportItem(e, null, null);
            var pd = placeMap[e.PlaceId.Value];
            return new EventViewportItem(e, (decimal?)pd.Latitude, (decimal?)pd.Longitude);
        }).ToList();

        return (items, ordered.Count);
    }

    public async Task<List<Event>> GetByIdsAsync(
        IEnumerable<EventId> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await Context.Events
            .Include(e => e.Categories)
            .Where(e => idList.Contains(e.Id))
            .ToListAsync(cancellationToken);
    }
}
