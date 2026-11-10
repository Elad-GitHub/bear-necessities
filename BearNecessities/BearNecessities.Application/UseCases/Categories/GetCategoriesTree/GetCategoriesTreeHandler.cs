using BearNecessities.Application.DTOs.Categories;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Interfaces;

namespace BearNecessities.Application.UseCases.Categories.GetCategoriesTree;

public sealed class GetCategoriesTreeHandler
    : IRequestHandler<GetCategoriesTreeRequest, GetCategoriesTreeResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCategoriesTreeHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetCategoriesTreeResponse> HandleAsync(
        GetCategoriesTreeRequest request,
        CancellationToken cancellationToken)
    {
        var allCategories = await _unitOfWork.Categories.GetAllAsync(cancellationToken);

        var dtoLookup = allCategories.ToDictionary(
            c => c.Id.Value,
            c => new CategoryTreeDto(c.Id.Value, c.Slug, c.Name, c.Icon, c.Color, c.SortOrder, []));

        var roots = new List<CategoryTreeDto>();

        foreach (var category in allCategories)
        {
            var dto = dtoLookup[category.Id.Value];
            if (category.ParentId is null)
                roots.Add(dto);
            else if (dtoLookup.TryGetValue(category.ParentId.Value, out var parentDto))
                parentDto.Children.Add(dto);
        }

        return new GetCategoriesTreeResponse(roots);
    }
}
