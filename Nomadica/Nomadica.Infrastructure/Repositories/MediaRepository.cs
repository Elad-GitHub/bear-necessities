using Microsoft.EntityFrameworkCore;
using Nomadica.Domain.Entities;
using Nomadica.Domain.Enums;
using Nomadica.Domain.Interfaces;
using Nomadica.Infrastructure.Persistence;

namespace Nomadica.Infrastructure.Repositories;

public class MediaRepository : BaseRepository<Media, Guid>, IMediaRepository
{
    public MediaRepository(NomadiaDbContext context) : base(context) { }

    public async Task<List<Media>> GetByOwnerAsync(
        MediaOwnerType ownerType,
        Guid ownerId,
        CancellationToken cancellationToken = default)
        => await Context.Media
            .Where(m => m.OwnerType == ownerType && m.OwnerId == ownerId)
            .OrderBy(m => m.SortOrder)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Media>> GetByOwnersAsync(
        MediaOwnerType ownerType,
        IEnumerable<Guid> ownerIds,
        CancellationToken cancellationToken = default)
    {
        var ids = ownerIds.ToList();
        return await Context.Media
            .Where(m => m.OwnerType == ownerType && ids.Contains(m.OwnerId))
            .OrderBy(m => m.SortOrder)
            .ToListAsync(cancellationToken);
    }
}
