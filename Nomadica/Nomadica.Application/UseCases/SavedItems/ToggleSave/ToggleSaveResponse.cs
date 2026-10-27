namespace Nomadica.Application.UseCases.SavedItems.ToggleSave;

public record ToggleSaveResponse(bool IsSaved, string? SaveType);
