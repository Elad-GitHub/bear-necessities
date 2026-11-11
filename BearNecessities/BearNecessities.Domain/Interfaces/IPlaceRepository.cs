using BearNecessities.Domain.Entities;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Domain.Interfaces;

public interface IPlaceRepository : IRepository<Place, PlaceId>
{
    Task<IReadOnlyList<Place>> FindByViewportAsync(
        GeoViewport viewport,
        IEnumerable<CategoryId>? categoryIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Place>> FindNearbyAsync(
        Coordinates center,
        double radiusKm,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Place> Items, int TotalCount)> FindByViewportPagedAsync(
        GeoViewport viewport,
        IEnumerable<CategoryId>? categoryIds,
        Guid? cursor,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Counts live places per square grid cell in the database, so zoomed-out maps never load every place.</summary>
    Task<IReadOnlyList<PlaceDensityCell>> GetDensityAsync(
        GeoViewport viewport,
        IReadOnlyCollection<Guid>? categoryIds,
        decimal cellSizeDegrees,
        CancellationToken cancellationToken = default);

    Task<Place?> GetByIdWithRelationsAsync(PlaceId id, CancellationToken cancellationToken = default);
    Task<List<Place>> SearchByTextAsync(string query, int limit, CancellationToken cancellationToken = default);
    Task<List<Place>> GetByOwnerAsync(UserId ownerId, CancellationToken cancellationToken = default);
    Task<List<Place>> FindByCountryAsync(string country, CancellationToken cancellationToken = default);
    Task<List<Place>> GetPendingAsync(int pageSize, Guid? cursor, CancellationToken cancellationToken = default);
    Task ReplaceExternalLinksAsync(PlaceId placeId, IEnumerable<PlaceExternalLink> newLinks, CancellationToken cancellationToken = default);
    Task<List<Place>> GetByIdsAsync(IEnumerable<PlaceId> ids, CancellationToken cancellationToken = default);
    Task<List<Place>> GetByIdsWithRelationsAsync(IEnumerable<PlaceId> ids, CancellationToken cancellationToken = default);
}
