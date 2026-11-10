using BearNecessities.Application.DTOs.Categories;

namespace BearNecessities.Application.UseCases.Categories.GetCategoriesTree;

public record GetCategoriesTreeResponse(IReadOnlyList<CategoryTreeDto> Categories);
