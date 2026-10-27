using Nomadica.Application.DTOs.Categories;

namespace Nomadica.Application.UseCases.Categories.GetCategoriesTree;

public record GetCategoriesTreeResponse(IReadOnlyList<CategoryTreeDto> Categories);
