using Nomadica.Domain.Enums;

namespace Nomadica.Application.UseCases.Media.GetMediaByOwner;

public record GetMediaByOwnerRequest(MediaOwnerType OwnerType, Guid OwnerId);
