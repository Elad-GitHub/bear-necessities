namespace Nomadica.Application.DTOs.Groups;

public record GroupSummaryDto(
    Guid Id,
    string Slug,
    string Name,
    string? Description,
    string? City,
    string? Country,
    decimal Latitude,
    decimal Longitude,
    int MemberCount,
    bool IsMember,
    string? MyRole);
