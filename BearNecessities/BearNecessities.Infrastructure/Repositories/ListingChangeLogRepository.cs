using Microsoft.EntityFrameworkCore;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Infrastructure.Persistence;

namespace BearNecessities.Infrastructure.Repositories;

public sealed class ListingChangeLogRepository : IListingChangeLogRepository
{
    private readonly BearNecessitiesDbContext _context;

    public ListingChangeLogRepository(BearNecessitiesDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ListingChangeLog log, CancellationToken cancellationToken = default)
        => await _context.ListingChangeLogs.AddAsync(log, cancellationToken);

    public async Task<List<ListingChangeLog>> GetLatestStatusChangesAsync(
        ListingChangeTargetType targetType,
        IEnumerable<Guid> targetIds,
        CancellationToken cancellationToken = default)
    {
        var idList = targetIds.ToList();
        if (idList.Count == 0) return [];

        var logs = await _context.ListingChangeLogs
            .Where(log => log.TargetType == targetType
                && log.ChangeType == ChangeType.StatusChange
                && idList.Contains(log.TargetId))
            .OrderByDescending(log => log.CreatedAt)
            .ToListAsync(cancellationToken);

        return logs
            .GroupBy(log => log.TargetId)
            .Select(group => group.First())
            .ToList();
    }
}
