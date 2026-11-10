using BearNecessities.Domain.Entities;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Domain.Interfaces;

public record EventViewportItem(Event Event, decimal? Latitude, decimal? Longitude);

public interface IEventRepository : IRepository<Event, EventId>
{
    Task<IReadOnlyList<Event>> FindUpcomingByPlaceAsync(
        PlaceId placeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Event>> FindByViewportAsync(
        GeoViewport viewport,
        IEnumerable<CategoryId>? categoryIds,
        CancellationToken cancellationToken = default);

    Task<Event?> GetByIdWithRelationsAsync(EventId id, CancellationToken cancellationToken = default);
    Task<List<Event>> GetByOwnerAsync(UserId ownerId, CancellationToken cancellationToken = default);

    Task<List<Event>> SearchUpcomingByTextAsync(string query, int limit, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<List<Event>> FindUpcomingByPlacesAsync(IEnumerable<PlaceId> placeIds, CancellationToken cancellationToken = default);
    Task<List<Event>> GetPendingAsync(int pageSize, Guid? cursor, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<EventViewportItem> Items, int TotalCount)> FindByViewportPagedAsync(
        GeoViewport viewport,
        IEnumerable<CategoryId>? categoryIds,
        DateTime? dateFrom,
        DateTime? dateTo,
        int pageSize,
        Guid? cursor,
        CancellationToken cancellationToken = default);
    Task<List<Event>> GetByIdsAsync(IEnumerable<EventId> ids, CancellationToken cancellationToken = default);
}
