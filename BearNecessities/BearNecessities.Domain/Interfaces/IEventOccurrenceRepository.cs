using BearNecessities.Domain.Entities;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Domain.Interfaces;

public interface IEventOccurrenceRepository
{
    Task AddRangeAsync(IEnumerable<EventOccurrence> occurrences, CancellationToken cancellationToken = default);
    Task DeleteFutureOccurrencesAsync(EventId eventId, DateTime fromUtc, CancellationToken cancellationToken = default);
    Task CancelFutureOccurrencesAsync(EventId eventId, DateTime fromUtc, CancellationToken cancellationToken = default);
    Task<List<EventOccurrence>> GetByEventAsync(EventId eventId, int pageSize, Guid? cursor, CancellationToken cancellationToken = default);
    Task<List<EventOccurrence>> GetUpcomingByEventAsync(EventId eventId, int count, CancellationToken cancellationToken = default);
}
