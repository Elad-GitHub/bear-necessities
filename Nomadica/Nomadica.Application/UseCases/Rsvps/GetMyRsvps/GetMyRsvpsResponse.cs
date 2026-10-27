using Nomadica.Application.DTOs.Rsvps;

namespace Nomadica.Application.UseCases.Rsvps.GetMyRsvps;

public record GetMyRsvpsResponse(IReadOnlyList<MyRsvpDto> Items, Guid? NextCursor);
