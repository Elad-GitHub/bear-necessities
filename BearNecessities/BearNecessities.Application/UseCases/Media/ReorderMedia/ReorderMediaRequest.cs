using BearNecessities.Domain.Enums;

namespace BearNecessities.Application.UseCases.Media.ReorderMedia;

public record ReorderMediaRequest(
    MediaOwnerType OwnerType,
    Guid OwnerId,
    List<Guid> OrderedMediaIds);
