using Nomadica.Domain.Entities;
using Nomadica.Domain.Enums;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Domain.Interfaces;

public interface IRsvpRepository : IRepository<Rsvp, Guid>
{
    Task<Rsvp?> GetUserRsvpForEventAsync(UserId userId, EventId eventId, Guid? occurrenceId, CancellationToken cancellationToken = default);
    Task<List<Rsvp>> GetByEventAsync(EventId eventId, int pageSize, Guid? cursor, CancellationToken cancellationToken = default);
    Task<List<Rsvp>> GetByUserAsync(UserId userId, int pageSize, Guid? cursor, CancellationToken cancellationToken = default);
    Task<int> GetCountByEventAsync(EventId eventId, RsvpStatus status, CancellationToken cancellationToken = default);
}
