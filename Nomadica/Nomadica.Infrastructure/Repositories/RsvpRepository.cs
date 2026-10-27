using Microsoft.EntityFrameworkCore;
using Nomadica.Domain.Entities;
using Nomadica.Domain.Enums;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;
using Nomadica.Infrastructure.Persistence;

namespace Nomadica.Infrastructure.Repositories;

public sealed class RsvpRepository : BaseRepository<Rsvp, Guid>, IRsvpRepository
{
    public RsvpRepository(NomadiaDbContext context) : base(context) { }

    public async Task<Rsvp?> GetUserRsvpForEventAsync(
        UserId userId,
        EventId eventId,
        Guid? occurrenceId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Rsvps
            .FirstOrDefaultAsync(
                r => r.UserId == userId && r.EventId == eventId && r.OccurrenceId == occurrenceId,
                cancellationToken);
    }

    public async Task<List<Rsvp>> GetByEventAsync(
        EventId eventId,
        int pageSize,
        Guid? cursor,
        CancellationToken cancellationToken = default)
    {
        var all = await Context.Rsvps
            .Where(r => r.EventId == eventId)
            .OrderBy(r => r.CreatedAt).ThenBy(r => r.Id)
            .ToListAsync(cancellationToken);

        var startIndex = 0;
        if (cursor.HasValue)
        {
            var idx = all.FindIndex(r => r.Id == cursor.Value);
            startIndex = idx >= 0 ? idx + 1 : 0;
        }

        return all.Skip(startIndex).Take(pageSize).ToList();
    }

    public async Task<List<Rsvp>> GetByUserAsync(
        UserId userId,
        int pageSize,
        Guid? cursor,
        CancellationToken cancellationToken = default)
    {
        var all = await Context.Rsvps
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id)
            .ToListAsync(cancellationToken);

        var startIndex = 0;
        if (cursor.HasValue)
        {
            var idx = all.FindIndex(r => r.Id == cursor.Value);
            startIndex = idx >= 0 ? idx + 1 : 0;
        }

        return all.Skip(startIndex).Take(pageSize).ToList();
    }

    public async Task<int> GetCountByEventAsync(
        EventId eventId,
        RsvpStatus status,
        CancellationToken cancellationToken = default)
    {
        return await Context.Rsvps
            .Where(r => r.EventId == eventId && r.Status == status)
            .SumAsync(r => (int?)r.PartySize, cancellationToken) ?? 0;
    }
}
