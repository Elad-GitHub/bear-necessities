using BearNecessities.Domain.Enums;

namespace BearNecessities.Application.UseCases.Rsvps.CreateOrUpdateRsvp;

public record CreateOrUpdateRsvpRequest(
    Guid EventId,
    RsvpStatus Status,
    int PartySize = 1,
    Guid? OccurrenceId = null);
