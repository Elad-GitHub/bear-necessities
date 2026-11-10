using BearNecessities.Domain.Enums;

namespace BearNecessities.Application.DTOs.Rsvps;

public record RsvpDto(
    Guid Id,
    Guid UserId,
    string UserDisplayName,
    string? UserAvatarUrl,
    RsvpStatus Status,
    int PartySize);
