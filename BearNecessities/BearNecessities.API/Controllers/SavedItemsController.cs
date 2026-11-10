using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BearNecessities.Application.UseCases.SavedItems.GetSavedItems;
using BearNecessities.Application.UseCases.SavedItems.ToggleSave;

namespace BearNecessities.API.Controllers;

[Authorize]
[Route("api/v1/saved")]
public class SavedItemsController : BaseApiController
{
    private readonly ToggleSaveHandler _toggleHandler;
    private readonly GetSavedItemsHandler _getHandler;

    public SavedItemsController(ToggleSaveHandler toggleHandler, GetSavedItemsHandler getHandler)
    {
        _toggleHandler = toggleHandler;
        _getHandler = getHandler;
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(
        [FromBody] ToggleSaveRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _toggleHandler.HandleAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetSaved(
        [FromQuery] string? targetType = null,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _getHandler.HandleAsync(
            new GetSavedItemsRequest(targetType, pageSize, cursor), cancellationToken);
        return Ok(response);
    }
}
