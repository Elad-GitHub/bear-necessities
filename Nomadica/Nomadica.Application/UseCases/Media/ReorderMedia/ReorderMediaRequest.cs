using Nomadica.Domain.Enums;

namespace Nomadica.Application.UseCases.Media.ReorderMedia;

public record ReorderMediaRequest(
    MediaOwnerType OwnerType,
    Guid OwnerId,
    List<Guid> OrderedMediaIds);
