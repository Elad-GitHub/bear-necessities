using BearNecessities.Application.DTOs.Saved;

namespace BearNecessities.Application.UseCases.SavedItems.GetSavedItems;

public record GetSavedItemsResponse(IReadOnlyList<SavedItemDto> Items, Guid? NextCursor);
