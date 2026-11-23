using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using BearNecessities.Application.UseCases.Feedback;

namespace BearNecessities.API.Controllers;

/// <summary>Bug reports and ideas sent from inside the app, and the admin inbox that reads them.</summary>
public class FeedbackController : BaseApiController
{
    private readonly SubmitFeedbackHandler _submitHandler;
    private readonly GetOpenFeedbackHandler _openHandler;
    private readonly HandleFeedbackHandler _handleHandler;

    public FeedbackController(SubmitFeedbackHandler submitHandler, GetOpenFeedbackHandler openHandler, HandleFeedbackHandler handleHandler)
    {
        _submitHandler = submitHandler;
        _openHandler = openHandler;
        _handleHandler = handleHandler;
    }

    // Open to signed-out visitors too, so it shares the strict limit of the sign-in endpoints
    [HttpPost]
    [EnableRateLimiting("auth-endpoints")]
    public async Task<IActionResult> Submit([FromBody] SubmitFeedbackRequest request, CancellationToken cancellationToken = default) =>
        Ok(await _submitHandler.HandleAsync(request, cancellationToken));

    [HttpGet("open")]
    [Authorize]
    public async Task<IActionResult> GetOpen(CancellationToken cancellationToken = default) =>
        Ok(await _openHandler.HandleAsync(new GetOpenFeedbackRequest(), cancellationToken));

    [HttpPost("{id:guid}/handled")]
    [Authorize]
    public async Task<IActionResult> MarkHandled(Guid id, CancellationToken cancellationToken = default) =>
        Ok(await _handleHandler.HandleAsync(new HandleFeedbackRequest(id), cancellationToken));
}
