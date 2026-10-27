using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nomadica.Application.UseCases.Places.ApprovePlace;
using Nomadica.Application.UseCases.Places.CreatePlace;
using Nomadica.Application.UseCases.Places.DeletePlace;
using Nomadica.Application.UseCases.Places.GetMyPlaces;
using Nomadica.Application.UseCases.Places.GetPendingPlaces;
using Nomadica.Application.UseCases.Places.GetPlaceById;
using Nomadica.Application.UseCases.Places.GetPlacesByViewport;
using Nomadica.Application.UseCases.Places.RejectPlace;
using Nomadica.Application.UseCases.Places.SearchPlaces;
using Nomadica.Application.UseCases.Places.UpdatePlace;

namespace Nomadica.API.Controllers;

public class PlacesController : BaseApiController
{
    private readonly GetPlacesByViewportHandler _viewportHandler;
    private readonly GetPlaceByIdHandler _byIdHandler;
    private readonly CreatePlaceHandler _createHandler;
    private readonly UpdatePlaceHandler _updateHandler;
    private readonly ApprovePlaceHandler _approveHandler;
    private readonly RejectPlaceHandler _rejectHandler;
    private readonly DeletePlaceHandler _deleteHandler;
    private readonly GetPendingPlacesHandler _pendingHandler;
    private readonly SearchPlacesHandler _searchHandler;
    private readonly GetMyPlacesHandler _myPlacesHandler;

    public PlacesController(
        GetPlacesByViewportHandler viewportHandler,
        GetPlaceByIdHandler byIdHandler,
        CreatePlaceHandler createHandler,
        UpdatePlaceHandler updateHandler,
        ApprovePlaceHandler approveHandler,
        RejectPlaceHandler rejectHandler,
        DeletePlaceHandler deleteHandler,
        GetPendingPlacesHandler pendingHandler,
        SearchPlacesHandler searchHandler,
        GetMyPlacesHandler myPlacesHandler)
    {
        _viewportHandler = viewportHandler;
        _byIdHandler = byIdHandler;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _approveHandler = approveHandler;
        _rejectHandler = rejectHandler;
        _deleteHandler = deleteHandler;
        _pendingHandler = pendingHandler;
        _searchHandler = searchHandler;
        _myPlacesHandler = myPlacesHandler;
    }

    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken = default)
    {
        var response = await _myPlacesHandler.HandleAsync(new GetMyPlacesRequest(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string query,
        [FromQuery] int limit = 8,
        CancellationToken cancellationToken = default)
    {
        var response = await _searchHandler.HandleAsync(
            new SearchPlacesRequest(query, limit), cancellationToken);
        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetByViewport(
        [FromQuery] decimal northEastLat,
        [FromQuery] decimal northEastLng,
        [FromQuery] decimal southWestLat,
        [FromQuery] decimal southWestLng,
        [FromQuery] Guid[]? categoryIds,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var request = new GetPlacesByViewportRequest(
            northEastLat, northEastLng, southWestLat, southWestLng,
            categoryIds, pageSize, cursor);

        var response = await _viewportHandler.HandleAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("pending")]
    [Authorize]
    public async Task<IActionResult> GetPending(
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _pendingHandler.HandleAsync(
            new GetPendingPlacesRequest(pageSize, cursor), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await _byIdHandler.HandleAsync(
            new GetPlaceByIdRequest(id), cancellationToken);

        return OkOrNotFound(response.Place);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        [FromBody] CreatePlaceRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _createHandler.HandleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.PlaceId }, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdatePlaceRequest request,
        CancellationToken cancellationToken = default)
    {
        var merged = request with { PlaceId = id };
        var response = await _updateHandler.HandleAsync(merged, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize]
    public async Task<IActionResult> Approve(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await _approveHandler.HandleAsync(
            new ApprovePlaceRequest(id), cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] RejectPlaceRequest request,
        CancellationToken cancellationToken = default)
    {
        var merged = request with { PlaceId = id };
        var response = await _rejectHandler.HandleAsync(merged, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await _deleteHandler.HandleAsync(new DeletePlaceRequest(id), cancellationToken);
        return NoContent();
    }
}
