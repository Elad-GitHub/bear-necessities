using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BearNecessities.Application.UseCases.Events.ApproveEvent;
using BearNecessities.Application.UseCases.Events.CancelEvent;
using BearNecessities.Application.UseCases.Events.CreateEvent;
using BearNecessities.Application.UseCases.Events.GetEventById;
using BearNecessities.Application.UseCases.Events.GetEventsByPlace;
using BearNecessities.Application.UseCases.Events.GetEventsByViewport;
using BearNecessities.Application.UseCases.Events.GetMyEvents;
using BearNecessities.Application.UseCases.Events.GetPendingEvents;
using BearNecessities.Application.UseCases.Events.RejectEvent;
using BearNecessities.Application.UseCases.Events.SearchEvents;
using BearNecessities.Application.UseCases.Events.UpdateEvent;

namespace BearNecessities.API.Controllers;

public class EventsController : BaseApiController
{
    private readonly CreateEventHandler _createHandler;
    private readonly UpdateEventHandler _updateHandler;
    private readonly ApproveEventHandler _approveHandler;
    private readonly RejectEventHandler _rejectHandler;
    private readonly CancelEventHandler _cancelHandler;
    private readonly GetEventByIdHandler _byIdHandler;
    private readonly GetEventsByViewportHandler _viewportHandler;
    private readonly GetEventsByPlaceHandler _byPlaceHandler;
    private readonly GetPendingEventsHandler _pendingHandler;
    private readonly GetMyEventsHandler _myEventsHandler;
    private readonly SearchEventsHandler _searchHandler;

    public EventsController(
        CreateEventHandler createHandler,
        UpdateEventHandler updateHandler,
        ApproveEventHandler approveHandler,
        RejectEventHandler rejectHandler,
        CancelEventHandler cancelHandler,
        GetEventByIdHandler byIdHandler,
        GetEventsByViewportHandler viewportHandler,
        GetEventsByPlaceHandler byPlaceHandler,
        GetPendingEventsHandler pendingHandler,
        GetMyEventsHandler myEventsHandler,
        SearchEventsHandler searchHandler)
    {
        _searchHandler = searchHandler;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _approveHandler = approveHandler;
        _rejectHandler = rejectHandler;
        _cancelHandler = cancelHandler;
        _byIdHandler = byIdHandler;
        _viewportHandler = viewportHandler;
        _byPlaceHandler = byPlaceHandler;
        _pendingHandler = pendingHandler;
        _myEventsHandler = myEventsHandler;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? query,
        [FromQuery] int limit = 6,
        CancellationToken cancellationToken = default)
    {
        var response = await _searchHandler.HandleAsync(new SearchEventsRequest(query, limit), cancellationToken);
        return Ok(response);
    }

    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken = default)
    {
        var response = await _myEventsHandler.HandleAsync(new GetMyEventsRequest(), cancellationToken);
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
            new GetPendingEventsRequest(pageSize, cursor), cancellationToken);
        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetByViewport(
        [FromQuery] decimal northEastLat,
        [FromQuery] decimal northEastLng,
        [FromQuery] decimal southWestLat,
        [FromQuery] decimal southWestLng,
        [FromQuery] Guid[]? categoryIds,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var request = new GetEventsByViewportRequest(
            northEastLat, northEastLng, southWestLat, southWestLng,
            categoryIds?.ToList(), dateFrom, dateTo, pageSize, cursor);

        var response = await _viewportHandler.HandleAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await _byIdHandler.HandleAsync(new GetEventByIdRequest(id), cancellationToken);
        return Ok(response);
    }

    [HttpGet("by-place/{placeId:guid}")]
    public async Task<IActionResult> GetByPlace(
        Guid placeId,
        CancellationToken cancellationToken = default)
    {
        var response = await _byPlaceHandler.HandleAsync(new GetEventsByPlaceRequest(placeId), cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        [FromBody] CreateEventRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _createHandler.HandleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.EventId }, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateEventRequest request,
        CancellationToken cancellationToken = default)
    {
        var merged = request with { EventId = id };
        var response = await _updateHandler.HandleAsync(merged, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize]
    public async Task<IActionResult> Approve(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await _approveHandler.HandleAsync(new ApproveEventRequest(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] RejectEventRequest request,
        CancellationToken cancellationToken = default)
    {
        var merged = request with { EventId = id };
        await _rejectHandler.HandleAsync(merged, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize]
    public async Task<IActionResult> Cancel(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await _cancelHandler.HandleAsync(new CancelEventRequest(id), cancellationToken);
        return NoContent();
    }
}
