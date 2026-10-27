using Nomadica.Application.DTOs.Rsvps;

namespace Nomadica.Application.UseCases.Rsvps.GetEventRsvps;

public record GetEventRsvpsResponse(
    IReadOnlyList<RsvpDto> Items,
    Guid? NextCursor,
    int GoingCount,
    int MaybeCount);
