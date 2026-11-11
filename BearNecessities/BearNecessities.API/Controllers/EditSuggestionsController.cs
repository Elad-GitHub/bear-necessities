using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BearNecessities.Application.UseCases.EditSuggestions;

namespace BearNecessities.API.Controllers;

/// <summary>Suggested edits to places and events, reviewed by admins in the moderation queue.</summary>
public class EditSuggestionsController : BaseApiController
{
    private readonly GetPendingEditSuggestionsHandler _pendingHandler;
    private readonly ReviewEditSuggestionHandler _reviewHandler;

    public EditSuggestionsController(GetPendingEditSuggestionsHandler pendingHandler, ReviewEditSuggestionHandler reviewHandler)
    {
        _pendingHandler = pendingHandler;
        _reviewHandler = reviewHandler;
    }

    [HttpGet("pending")]
    [Authorize]
    public async Task<IActionResult> GetPending(CancellationToken cancellationToken = default) =>
        Ok(await _pendingHandler.HandleAsync(new GetPendingEditSuggestionsRequest(), cancellationToken));

    [HttpPost("{id:guid}/approve")]
    [Authorize]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken = default) =>
        Ok(await _reviewHandler.HandleAsync(new ReviewEditSuggestionRequest(id, IsApproved: true), cancellationToken));

    [HttpPost("{id:guid}/reject")]
    [Authorize]
    public async Task<IActionResult> Reject(Guid id, CancellationToken cancellationToken = default) =>
        Ok(await _reviewHandler.HandleAsync(new ReviewEditSuggestionRequest(id, IsApproved: false), cancellationToken));
}
