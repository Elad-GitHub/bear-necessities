using BearNecessities.Application.DTOs.Rsvps;

namespace BearNecessities.Application.UseCases.Rsvps.GetEventRsvps;

public record GetEventRsvpsResponse(
    IReadOnlyList<RsvpDto> Items,
    Guid? NextCursor,
    int GoingCount,
    int MaybeCount);
