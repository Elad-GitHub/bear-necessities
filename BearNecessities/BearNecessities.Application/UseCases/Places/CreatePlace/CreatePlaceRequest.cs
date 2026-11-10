using BearNecessities.Application.DTOs.Places;

namespace BearNecessities.Application.UseCases.Places.CreatePlace;

public record CreatePlaceRequest(
    string Name,
    string? Description,
    decimal Latitude,
    decimal Longitude,
    string? Address,
    string? City,
    string? Country,
    string? CountryCode,
    string Timezone,
    bool IsPermanent,
    List<Guid> CategoryIds,
    List<CreateExternalLinkDto>? ExternalLinks);
