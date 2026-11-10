using BearNecessities.Domain.Enums;

namespace BearNecessities.Application.UseCases.Media.GetMediaByOwner;

public record GetMediaByOwnerRequest(MediaOwnerType OwnerType, Guid OwnerId);
