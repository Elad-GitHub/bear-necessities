using BearNecessities.Domain.Enums;

namespace BearNecessities.Application.UseCases.Rsvps.CreateOrUpdateRsvp;

public record CreateOrUpdateRsvpResponse(
    Guid RsvpId,
    RsvpStatus Status,
    int PartySize,
    bool Created);
