using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BearNecessities.Application.UseCases.Users.GetPendingMembers;
using BearNecessities.Application.UseCases.Users.ReviewMembership;
using BearNecessities.Application.UseCases.Users.MyProfile;

namespace BearNecessities.API.Controllers;

public class UsersController : BaseApiController
{
    private readonly GetPendingMembersHandler _pendingHandler;
    private readonly ReviewMembershipHandler _reviewHandler;
    private readonly GetMyProfileHandler _getMyProfileHandler;
    private readonly UpdateMyProfileHandler _updateMyProfileHandler;

    public UsersController(
        GetPendingMembersHandler pendingHandler,
        ReviewMembershipHandler reviewHandler,
        GetMyProfileHandler getMyProfileHandler,
        UpdateMyProfileHandler updateMyProfileHandler)
    {
        _pendingHandler = pendingHandler;
        _reviewHandler = reviewHandler;
        _getMyProfileHandler = getMyProfileHandler;
        _updateMyProfileHandler = updateMyProfileHandler;
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken = default) =>
        Ok(await _getMyProfileHandler.HandleAsync(new GetMyProfileRequest(), cancellationToken));

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateMyProfileRequest request, CancellationToken cancellationToken = default) =>
        Ok(await _updateMyProfileHandler.HandleAsync(request, cancellationToken));

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
