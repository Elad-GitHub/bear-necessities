using Nomadica.Domain.Entities;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Domain.Interfaces;

public interface ISavedItemRepository
{
    Task SaveAsync(SavedItem item, CancellationToken cancellationToken = default);
    Task<SavedItem?> GetAsync(UserId userId, string targetType, Guid targetId, CancellationToken cancellationToken = default);
    void Remove(SavedItem item);
    Task<bool> UnsaveAsync(UserId userId, string targetType, Guid targetId, CancellationToken cancellationToken = default);
    Task<List<SavedItem>> GetByUserAsync(UserId userId, string? targetType, int pageSize, Guid? cursor, CancellationToken cancellationToken = default);
    Task<bool> IsSavedAsync(UserId userId, string targetType, Guid targetId, CancellationToken cancellationToken = default);
}
