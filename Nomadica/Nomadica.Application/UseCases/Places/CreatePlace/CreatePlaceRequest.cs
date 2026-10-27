using Nomadica.Application.DTOs.Places;

namespace Nomadica.Application.UseCases.Places.CreatePlace;

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
