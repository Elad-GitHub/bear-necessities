using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;

namespace BearNecessities.Domain.Interfaces;

public interface IMediaRepository : IRepository<Media, Guid>
{
    Task<List<Media>> GetByOwnerAsync(
        MediaOwnerType ownerType,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Media>> GetByOwnersAsync(
        MediaOwnerType ownerType,
        IEnumerable<Guid> ownerIds,
        CancellationToken cancellationToken = default);

    Task<List<Media>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<List<Media>> GetApprovedByReviewsAsync(IEnumerable<Guid> reviewIds, CancellationToken cancellationToken = default);
    Task<List<Media>> GetPendingAsync(CancellationToken cancellationToken = default);
}
