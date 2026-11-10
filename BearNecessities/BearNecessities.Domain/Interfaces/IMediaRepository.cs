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
}
