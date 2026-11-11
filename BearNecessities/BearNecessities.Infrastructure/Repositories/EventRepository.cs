using Microsoft.EntityFrameworkCore;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;
using BearNecessities.Infrastructure.Persistence;

namespace BearNecessities.Infrastructure.Repositories;

public class EventRepository : BaseRepository<Event, EventId>, IEventRepository
{
    public EventRepository(BearNecessitiesDbContext context) : base(context) { }

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

    public async Task<IReadOnlyDictionary<PlaceId, PlaceNextEvent>> FindNextEventByPlacesAsync(
        IEnumerable<PlaceId> placeIds, DateTime fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default)
    {
        var idList = placeIds.ToList();
        if (idList.Count == 0) return new Dictionary<PlaceId, PlaceNextEvent>();

        var liveAtPlaces = Context.Events.Where(e => e.Status == ListingStatus.Live
            && e.PlaceId != null && idList.Contains(e.PlaceId!));
        var until = toUtc ?? DateTime.MaxValue;

        var scheduled = await (
            from o in Context.EventOccurrences
            join e in liveAtPlaces on o.RecurringEventId equals e.Id
            where o.Status != ListingStatus.Cancelled && o.StartAt <= until
                && (o.EndAt >= fromUtc || (o.EndAt == null && o.StartAt >= fromUtc))
            select new NextCandidate(e.PlaceId!, e.Id, e.Title, o.StartAt)).ToListAsync(cancellationToken);

        // One-time events may carry only their own dates, without a scheduled occurrence
        var oneTime = await liveAtPlaces
            .Where(e => e.EventType == EventType.OneTime && e.StartAt != null && e.StartAt <= until
                && (e.EndAt >= fromUtc || (e.EndAt == null && e.StartAt >= fromUtc)))
            .Select(e => new NextCandidate(e.PlaceId!, e.Id, e.Title, e.StartAt))
            .ToListAsync(cancellationToken);

        var ongoing = await liveAtPlaces
            .Where(e => e.EventType == EventType.Permanent)
            .Select(e => new NextCandidate(e.PlaceId!, e.Id, e.Title, null))
            .ToListAsync(cancellationToken);

        // A dated happening beats an ongoing one: "Boom Festival · 22 Jul" says more than "Ongoing"
        var next = new Dictionary<PlaceId, PlaceNextEvent>();
        foreach (var candidate in ongoing.Concat(scheduled.Concat(oneTime).OrderByDescending(item => item.StartAt)))
            next[candidate.PlaceId] = new PlaceNextEvent(candidate.EventId, candidate.Title, candidate.StartAt);
        return next;
    }

    private sealed record NextCandidate(PlaceId PlaceId, EventId EventId, string Title, DateTime? StartAt);

    public async Task<List<Event>> GetByOwnerAsync(
        UserId ownerId,
        CancellationToken cancellationToken = default)
        => await Context.Events
            .Include(e => e.Categories)
            .Where(e => e.OwnerId == ownerId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

    // Live events matched by title; undated (permanent/recurring) ones count as upcoming
    public async Task<List<Event>> SearchUpcomingByTextAsync(
        string query,
        int limit,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var pattern = $"%{query}%";
        return await Context.Events
            .Include(e => e.Categories)
            .Where(e => e.Status == ListingStatus.Live
                && EF.Functions.Like(e.Title, pattern)
                && (e.StartAt == null || (e.EndAt ?? e.StartAt) >= nowUtc))
            .OrderBy(e => e.StartAt == null)
            .ThenBy(e => e.StartAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

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
