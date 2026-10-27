using Nomadica.Application.DTOs.Places;

namespace Nomadica.Application.UseCases.Places.UpdatePlace;

public record UpdatePlaceRequest(
    Guid PlaceId,
    string? Name,
    string? Description,
    decimal? Latitude,
    decimal? Longitude,
    string? Address,
    string? City,
    string? Country,
    string? CountryCode,
    string? Timezone,
    bool? IsPermanent,
    List<Guid>? CategoryIds,
    List<CreateExternalLinkDto>? ExternalLinks);
