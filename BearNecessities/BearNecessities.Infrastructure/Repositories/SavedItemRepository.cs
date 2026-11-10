using Microsoft.EntityFrameworkCore;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;
using BearNecessities.Infrastructure.Persistence;

namespace BearNecessities.Infrastructure.Repositories;

public sealed class SavedItemRepository : ISavedItemRepository
{
    private readonly BearNecessitiesDbContext _context;

    public SavedItemRepository(BearNecessitiesDbContext context)
    {
        _context = context;
    }

    public async Task SaveAsync(SavedItem item, CancellationToken cancellationToken = default)
        => await _context.SavedItems.AddAsync(item, cancellationToken);

    public async Task<SavedItem?> GetAsync(
        UserId userId,
        string targetType,
        Guid targetId,
        CancellationToken cancellationToken = default)
        => await _context.SavedItems
            .FirstOrDefaultAsync(
                s => s.UserId == userId && s.TargetType == targetType && s.TargetId == targetId,
                cancellationToken);

    public void Remove(SavedItem item)
        => _context.SavedItems.Remove(item);

    public async Task<bool> UnsaveAsync(
        UserId userId,
        string targetType,
        Guid targetId,
        CancellationToken cancellationToken = default)
    {
        var item = await _context.SavedItems
            .FirstOrDefaultAsync(
                s => s.UserId == userId && s.TargetType == targetType && s.TargetId == targetId,
                cancellationToken);

        if (item is null) return false;
        _context.SavedItems.Remove(item);
        return true;
    }

    public async Task<List<SavedItem>> GetByUserAsync(
        UserId userId,
        string? targetType,
        int pageSize,
        Guid? cursor,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SavedItems.Where(s => s.UserId == userId);

        if (!string.IsNullOrEmpty(targetType))
            query = query.Where(s => s.TargetType == targetType);

        var all = await query
            .OrderByDescending(s => s.SavedAt)
            .ToListAsync(cancellationToken);

        var startIndex = 0;
        if (cursor.HasValue)
        {
            var idx = all.FindIndex(s => s.TargetId == cursor.Value);
            startIndex = idx >= 0 ? idx + 1 : 0;
        }

        return all.Skip(startIndex).Take(pageSize).ToList();
    }

    public async Task<bool> IsSavedAsync(
        UserId userId,
        string targetType,
        Guid targetId,
        CancellationToken cancellationToken = default)
        => await _context.SavedItems
            .AnyAsync(
                s => s.UserId == userId && s.TargetType == targetType && s.TargetId == targetId,
                cancellationToken);
}
