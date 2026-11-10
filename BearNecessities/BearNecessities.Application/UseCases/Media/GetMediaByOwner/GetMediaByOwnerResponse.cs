using BearNecessities.Application.DTOs;

namespace BearNecessities.Application.UseCases.Media.GetMediaByOwner;

public record GetMediaByOwnerResponse(IReadOnlyList<MediaDto> Items);
