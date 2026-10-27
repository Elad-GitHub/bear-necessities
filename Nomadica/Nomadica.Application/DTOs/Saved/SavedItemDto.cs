namespace Nomadica.Application.DTOs.Saved;

public record SavedItemDto(
    string TargetType,
    Guid TargetId,
    string SaveType,
    DateTime SavedAt,
    object? Summary);
