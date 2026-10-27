using Nomadica.Domain.Entities;
using Nomadica.Domain.Enums;

namespace Nomadica.Domain.Interfaces;

public interface IListingChangeLogRepository
{
    Task AddAsync(ListingChangeLog log, CancellationToken cancellationToken = default);

    Task<List<ListingChangeLog>> GetLatestStatusChangesAsync(
        ListingChangeTargetType targetType,
        IEnumerable<Guid> targetIds,
        CancellationToken cancellationToken = default);
}
