using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;

namespace BearNecessities.Domain.Interfaces;

public interface IListingChangeLogRepository
{
    Task AddAsync(ListingChangeLog log, CancellationToken cancellationToken = default);

    Task<List<ListingChangeLog>> GetLatestStatusChangesAsync(
        ListingChangeTargetType targetType,
        IEnumerable<Guid> targetIds,
        CancellationToken cancellationToken = default);
}
