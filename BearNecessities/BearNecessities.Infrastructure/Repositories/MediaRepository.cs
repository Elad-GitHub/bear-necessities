using Microsoft.EntityFrameworkCore;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Infrastructure.Persistence;

namespace BearNecessities.Infrastructure.Repositories;

public class MediaRepository : BaseRepository<Media, Guid>, IMediaRepository
{
    public MediaRepository(BearNecessitiesDbContext context) : base(context) { }

    public async Task<List<Media>> GetByOwnerAsync(
        MediaOwnerType ownerType,
        Guid ownerId,
        CancellationToken cancellationToken = default)
        => await Context.Media
            .Where(m => m.OwnerType == ownerType && m.OwnerId == ownerId)
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Media>> GetByOwnersAsync(
        MediaOwnerType ownerType,
        IEnumerable<Guid> ownerIds,
        CancellationToken cancellationToken = default)
    {
        var ids = ownerIds.ToList();
        return await Context.Media
            .Where(m => m.OwnerType == ownerType && ids.Contains(m.OwnerId))
            // Oldest first among equals, so the photo a place started with stays its banner
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Media>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await Context.Media.Where(m => idList.Contains(m.Id)).ToListAsync(cancellationToken);
    }

    public async Task<List<Media>> GetApprovedByReviewsAsync(IEnumerable<Guid> reviewIds, CancellationToken cancellationToken = default)
    {
        var idList = reviewIds.Select(id => (Guid?)id).ToList();
        return await Context.Media
            .Where(m => m.ModerationStatus == ModerationStatus.Approved && idList.Contains(m.ReviewId))
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Media>> GetPendingAsync(CancellationToken cancellationToken = default)
        => await Context.Media
            .Where(m => m.ModerationStatus == ModerationStatus.Pending && m.OwnerType == MediaOwnerType.Place)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
}
