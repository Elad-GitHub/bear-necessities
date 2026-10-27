using Nomadica.Domain.Enums;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Domain.Entities;

public sealed class Rsvp : BaseEntity<Guid>
{
    private Rsvp() { }

    public UserId UserId { get; private set; } = default!;
    public EventId EventId { get; private set; } = default!;
    public Guid? OccurrenceId { get; private set; }
    public RsvpStatus Status { get; private set; }
    public int PartySize { get; private set; }

    public static Rsvp Create(UserId userId, EventId eventId, RsvpStatus status, int partySize = 1, Guid? occurrenceId = null)
    {
        if (partySize < 1 || partySize > 10)
            throw new ArgumentOutOfRangeException(nameof(partySize), "Party size must be between 1 and 10.");

        return new Rsvp
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventId = eventId,
            OccurrenceId = occurrenceId,
            Status = status,
            PartySize = partySize
        };
    }

    public void Update(RsvpStatus status, int partySize)
    {
        if (partySize < 1 || partySize > 10)
            throw new ArgumentOutOfRangeException(nameof(partySize), "Party size must be between 1 and 10.");
        Status = status;
        PartySize = partySize;
    }
}
