using Nomadica.Application.DTOs;

namespace Nomadica.Application.UseCases.Media.GetMediaByOwner;

public record GetMediaByOwnerResponse(IReadOnlyList<MediaDto> Items);
