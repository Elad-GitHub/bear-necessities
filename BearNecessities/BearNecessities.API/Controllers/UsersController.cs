using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BearNecessities.Application.UseCases.Users.GetPendingMembers;
using BearNecessities.Application.UseCases.Users.ReviewMembership;

namespace BearNecessities.API.Controllers;

public class UsersController : BaseApiController
{
    private readonly GetPendingMembersHandler _pendingHandler;
    private readonly ReviewMembershipHandler _reviewHandler;

    public UsersController(
        GetPendingMembersHandler pendingHandler,
        ReviewMembershipHandler reviewHandler)
    {
        _pendingHandler = pendingHandler;
        _reviewHandler = reviewHandler;
    }

    [HttpGet("pending")]
    [Authorize]
    public async Task<IActionResult> GetPending(CancellationToken cancellationToken = default)
    {
        var response = await _pendingHandler.HandleAsync(new GetPendingMembersRequest(), cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id:guid}/approve-membership")]
    [Authorize]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _reviewHandler.HandleAsync(new ReviewMembershipRequest(id, true), cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id:guid}/reject-membership")]
    [Authorize]
    public async Task<IActionResult> Reject(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _reviewHandler.HandleAsync(new ReviewMembershipRequest(id, false), cancellationToken);
        return Ok(response);
    }
}
