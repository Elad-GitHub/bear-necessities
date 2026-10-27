using Microsoft.EntityFrameworkCore;
using Nomadica.Domain.Entities;
using Nomadica.Domain.Enums;
using Nomadica.Domain.Interfaces;
using Nomadica.Infrastructure.Persistence;

namespace Nomadica.Infrastructure.Repositories;

public sealed class ListingChangeLogRepository : IListingChangeLogRepository
{
    private readonly NomadiaDbContext _context;

    public ListingChangeLogRepository(NomadiaDbContext context)
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
