namespace Nomadica.Application.UseCases.SavedItems.ToggleSave;

public record ToggleSaveRequest(string TargetType, Guid TargetId, string SaveType = "WantToGo");
