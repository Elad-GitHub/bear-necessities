using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;
using BearNecessities.Infrastructure.Persistence;

namespace BearNecessities.Infrastructure.Services;

public sealed class OccurrenceRefreshService : IOccurrenceRefreshService
{
    private readonly BearNecessitiesDbContext _context;
    private readonly IOccurrenceExpander _expander;
    private readonly IEventRepository _eventRepository;
    private readonly IEventOccurrenceRepository _occurrenceRepository;
    private readonly ILogger<OccurrenceRefreshService> _logger;

    public OccurrenceRefreshService(
        BearNecessitiesDbContext context,
        IOccurrenceExpander expander,
        IEventRepository eventRepository,
        IEventOccurrenceRepository occurrenceRepository,
        ILogger<OccurrenceRefreshService> logger)
    {
        _context = context;
        _expander = expander;
        _eventRepository = eventRepository;
        _occurrenceRepository = occurrenceRepository;
        _logger = logger;
    }

    public async Task<int> RefreshWindowAsync(CancellationToken cancellationToken = default)
    {
        var eventCount = 0;
        var totalOccurrencesCreated = 0;

        try
        {
            var now = DateTime.UtcNow;
            var window60DaysAhead = now.AddDays(60);
            var window75DaysAhead = now.AddDays(75);
            var window90DaysAhead = now.AddDays(90);

            var recurringLiveEvents = await _context.Events
                .Where(e => e.EventType == EventType.Recurring && e.Status == ListingStatus.Live && e.RRule != null)
                .ToListAsync(cancellationToken);

            foreach (var evt in recurringLiveEvents)
            {
                var eventId = evt.Id;
                var latestOccurrence = await _context.EventOccurrences
                    .Where(o => o.RecurringEventId == eventId)
                    .OrderByDescending(o => o.StartAt)
                    .FirstOrDefaultAsync(cancellationToken);

                var expandFrom = latestOccurrence?.StartAt ?? evt.CreatedAt;

                if (expandFrom >= window75DaysAhead)
                {
                    _logger.LogDebug("Event {EventId} already has occurrences beyond 75 days; skipping", eventId.Value);
                    continue;
                }

                if (expandFrom >= window60DaysAhead)
                {
                    _logger.LogDebug("Event {EventId} has recent occurrences; skipping this cycle", eventId.Value);
                    continue;
                }

                var occurrences = _expander.ExpandOccurrences(
                    evt.RRule!,
                    expandFrom,
                    evt.Timezone,
                    (int)(window90DaysAhead - expandFrom).TotalDays);

                if (occurrences.Count > 0)
                {
                    var newOccurrences = occurrences
                        .Select(o => EventOccurrence.Create(eventId, o.StartUtc, o.EndUtc))
                        .ToList();

                    await _occurrenceRepository.AddRangeAsync(newOccurrences, cancellationToken);
                    totalOccurrencesCreated += newOccurrences.Count;
                    eventCount++;

                    _logger.LogInformation("Event {EventId} expanded with {OccurrenceCount} occurrences",
                        eventId.Value, newOccurrences.Count);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Occurrence refresh complete: {EventCount} events processed, {OccurrenceCount} occurrences created",
                eventCount, totalOccurrencesCreated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during occurrence refresh");
            throw;
        }

        return eventCount;
    }
}
