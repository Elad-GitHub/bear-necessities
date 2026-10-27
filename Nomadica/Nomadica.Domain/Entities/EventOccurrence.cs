using Nomadica.Domain.Enums;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Domain.Entities;

public sealed class EventOccurrence : BaseEntity<Guid>
{
    private EventOccurrence() { }

    public EventId RecurringEventId { get; private set; } = default!;
    public DateTime StartAt { get; private set; }
    public DateTime? EndAt { get; private set; }
    public ListingStatus Status { get; private set; }
    public string? OverrideData { get; private set; }

    public static EventOccurrence Create(EventId recurringEventId, DateTime startAt, DateTime? endAt = null)
    {
        if (endAt.HasValue && endAt.Value <= startAt)
            throw new ArgumentException("EndAt must be after StartAt.", nameof(endAt));

        return new EventOccurrence
        {
            Id = Guid.NewGuid(),
            RecurringEventId = recurringEventId,
            StartAt = startAt,
            EndAt = endAt,
            Status = ListingStatus.Live
        };
    }
}
