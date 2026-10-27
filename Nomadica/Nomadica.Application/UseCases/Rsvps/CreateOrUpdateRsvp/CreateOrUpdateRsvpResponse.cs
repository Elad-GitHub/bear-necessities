using Nomadica.Domain.Enums;

namespace Nomadica.Application.UseCases.Rsvps.CreateOrUpdateRsvp;

public record CreateOrUpdateRsvpResponse(
    Guid RsvpId,
    RsvpStatus Status,
    int PartySize,
    bool Created);
