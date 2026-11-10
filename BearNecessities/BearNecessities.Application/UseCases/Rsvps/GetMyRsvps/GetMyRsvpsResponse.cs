using BearNecessities.Application.DTOs.Rsvps;

namespace BearNecessities.Application.UseCases.Rsvps.GetMyRsvps;

public record GetMyRsvpsResponse(IReadOnlyList<MyRsvpDto> Items, Guid? NextCursor);
