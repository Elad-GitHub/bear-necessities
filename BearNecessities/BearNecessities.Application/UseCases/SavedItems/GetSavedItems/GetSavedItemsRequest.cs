namespace BearNecessities.Application.UseCases.SavedItems.GetSavedItems;

public record GetSavedItemsRequest(string? TargetType = null, int PageSize = 20, Guid? Cursor = null);
