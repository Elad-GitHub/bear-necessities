using Nomadica.Application.DTOs.Saved;

namespace Nomadica.Application.UseCases.SavedItems.GetSavedItems;

public record GetSavedItemsResponse(IReadOnlyList<SavedItemDto> Items, Guid? NextCursor);
