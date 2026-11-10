using Microsoft.EntityFrameworkCore;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;
using BearNecessities.Infrastructure.Persistence;

namespace BearNecessities.Infrastructure.Repositories;

public sealed class EventOccurrenceRepository : IEventOccurrenceRepository
{
    private readonly BearNecessitiesDbContext _context;

    public EventOccurrenceRepository(BearNecessitiesDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(IEnumerable<EventOccurrence> occurrences, CancellationToken cancellationToken = default)
        => await _context.EventOccurrences.AddRangeAsync(occurrences, cancellationToken);

    public async Task DeleteFutureOccurrencesAsync(EventId eventId, DateTime fromUtc, CancellationToken cancellationToken = default)
    {
        await _context.EventOccurrences
            .Where(o => o.RecurringEventId == eventId
                && o.StartAt >= fromUtc
                && o.Status != ListingStatus.Cancelled)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task CancelFutureOccurrencesAsync(EventId eventId, DateTime fromUtc, CancellationToken cancellationToken = default)
    {
        await _context.EventOccurrences
            .Where(o => o.RecurringEventId == eventId
                && o.StartAt >= fromUtc
                && o.Status != ListingStatus.Cancelled)
            .ExecuteUpdateAsync(
                s => s.SetProperty(o => o.Status, ListingStatus.Cancelled),
                cancellationToken);
    }

    public async Task<List<EventOccurrence>> GetByEventAsync(
        EventId eventId,
        int pageSize,
        Guid? cursor,
        CancellationToken cancellationToken = default)
    {
        var all = await _context.EventOccurrences
            .Where(o => o.RecurringEventId == eventId)
            .OrderBy(o => o.StartAt)
            .ToListAsync(cancellationToken);

        if (cursor.HasValue)
        {
            var idx = all.FindIndex(o => o.Id == cursor.Value);
            if (idx >= 0)
                all = all.Skip(idx + 1).ToList();
        }

        return all.Take(pageSize).ToList();
    }

    public async Task<List<EventOccurrence>> GetUpcomingByEventAsync(
        EventId eventId,
        int count,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.EventOccurrences
            .Where(o => o.RecurringEventId == eventId
                && o.StartAt >= now
                && o.Status != ListingStatus.Cancelled)
            .OrderBy(o => o.StartAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
