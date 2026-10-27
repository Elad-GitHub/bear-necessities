using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nomadica.Application.UseCases.Rsvps.CancelRsvp;
using Nomadica.Application.UseCases.Rsvps.CreateOrUpdateRsvp;
using Nomadica.Application.UseCases.Rsvps.GetEventRsvps;
using Nomadica.Application.UseCases.Rsvps.GetMyRsvps;

namespace Nomadica.API.Controllers;

public class RsvpsController : BaseApiController
{
    private readonly CreateOrUpdateRsvpHandler _createOrUpdateHandler;
    private readonly CancelRsvpHandler _cancelHandler;
    private readonly GetEventRsvpsHandler _eventRsvpsHandler;
    private readonly GetMyRsvpsHandler _myRsvpsHandler;

    public RsvpsController(
        CreateOrUpdateRsvpHandler createOrUpdateHandler,
        CancelRsvpHandler cancelHandler,
        GetEventRsvpsHandler eventRsvpsHandler,
        GetMyRsvpsHandler myRsvpsHandler)
    {
        _createOrUpdateHandler = createOrUpdateHandler;
        _cancelHandler = cancelHandler;
        _eventRsvpsHandler = eventRsvpsHandler;
        _myRsvpsHandler = myRsvpsHandler;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateOrUpdate(
        [FromBody] CreateOrUpdateRsvpRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _createOrUpdateHandler.HandleAsync(request, cancellationToken);
        return response.Created ? Created(string.Empty, response) : Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken = default)
    {
        await _cancelHandler.HandleAsync(new CancelRsvpRequest(id), cancellationToken);
        return NoContent();
    }

    [HttpGet("event/{eventId:guid}")]
    public async Task<IActionResult> GetByEvent(
        Guid eventId,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _eventRsvpsHandler.HandleAsync(
            new GetEventRsvpsRequest(eventId, pageSize, cursor), cancellationToken);
        return Ok(response);
    }

    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> GetMine(
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _myRsvpsHandler.HandleAsync(
            new GetMyRsvpsRequest(pageSize, cursor), cancellationToken);
        return Ok(response);
    }
}
