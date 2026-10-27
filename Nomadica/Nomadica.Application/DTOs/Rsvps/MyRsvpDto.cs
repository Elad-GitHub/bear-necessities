using Nomadica.Domain.Enums;

namespace Nomadica.Application.DTOs.Rsvps;

public record MyRsvpDto(
    Guid Id,
    Guid EventId,
    string EventTitle,
    DateTime? EventStartAt,
    string? PlaceName,
    RsvpStatus Status,
    int PartySize);
