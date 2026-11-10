namespace BearNecessities.Application.DTOs.Categories;

public record CategoryTreeDto(
    Guid Id,
    string Slug,
    string Name,
    string? Icon,
    string? Color,
    int SortOrder,
    List<CategoryTreeDto> Children);
