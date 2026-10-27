using Microsoft.AspNetCore.Mvc;
using Nomadica.Application.UseCases.Categories.GetCategoriesTree;

namespace Nomadica.API.Controllers;

public class CategoriesController : BaseApiController
{
    private readonly GetCategoriesTreeHandler _handler;

    public CategoriesController(GetCategoriesTreeHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    public async Task<IActionResult> GetTree(CancellationToken cancellationToken = default)
    {
        var response = await _handler.HandleAsync(new GetCategoriesTreeRequest(), cancellationToken);
        return Ok(response.Categories);
    }
}
