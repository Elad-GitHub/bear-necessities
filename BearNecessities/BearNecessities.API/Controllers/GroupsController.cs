using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BearNecessities.Application.UseCases.Groups.CreateGroup;
using BearNecessities.Application.UseCases.Groups.CreateGroupPost;
using BearNecessities.Application.UseCases.Groups.GetGroupById;
using BearNecessities.Application.UseCases.Groups.GetGroupPosts;
using BearNecessities.Application.UseCases.Groups.GetGroups;
using BearNecessities.Application.UseCases.Groups.GetMyGroups;
using BearNecessities.Application.UseCases.Groups.JoinGroup;
using BearNecessities.Application.UseCases.Groups.LeaveGroup;

namespace BearNecessities.API.Controllers;

public class GroupsController : BaseApiController
{
    private readonly CreateGroupHandler _createHandler;
    private readonly GetGroupsHandler _getGroupsHandler;
    private readonly GetGroupByIdHandler _byIdHandler;
    private readonly GetMyGroupsHandler _myGroupsHandler;
    private readonly JoinGroupHandler _joinHandler;
    private readonly LeaveGroupHandler _leaveHandler;
    private readonly GetGroupPostsHandler _postsHandler;
    private readonly CreateGroupPostHandler _createPostHandler;

    public GroupsController(
        CreateGroupHandler createHandler,
        GetGroupsHandler getGroupsHandler,
        GetGroupByIdHandler byIdHandler,
        GetMyGroupsHandler myGroupsHandler,
        JoinGroupHandler joinHandler,
        LeaveGroupHandler leaveHandler,
        GetGroupPostsHandler postsHandler,
        CreateGroupPostHandler createPostHandler)
    {
        _createHandler = createHandler;
        _getGroupsHandler = getGroupsHandler;
        _byIdHandler = byIdHandler;
        _myGroupsHandler = myGroupsHandler;
        _joinHandler = joinHandler;
        _leaveHandler = leaveHandler;
        _postsHandler = postsHandler;
        _createPostHandler = createPostHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetGroups(
        [FromQuery] string? query = null,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var response = await _getGroupsHandler.HandleAsync(new GetGroupsRequest(query, limit), cancellationToken);
        return Ok(response);
    }

    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken = default)
    {
        var response = await _myGroupsHandler.HandleAsync(new GetMyGroupsRequest(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _byIdHandler.HandleAsync(new GetGroupByIdRequest(id), cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        [FromBody] CreateGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _createHandler.HandleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Group.Id }, response);
    }

    [HttpPost("{id:guid}/join")]
    [Authorize]
    public async Task<IActionResult> Join(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _joinHandler.HandleAsync(new JoinGroupRequest(id), cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id:guid}/leave")]
    [Authorize]
    public async Task<IActionResult> Leave(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _leaveHandler.HandleAsync(new LeaveGroupRequest(id), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}/posts")]
    public async Task<IActionResult> GetPosts(
        Guid id,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _postsHandler.HandleAsync(
            new GetGroupPostsRequest(id, pageSize, cursor), cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id:guid}/posts")]
    [Authorize]
    public async Task<IActionResult> CreatePost(
        Guid id,
        [FromBody] CreateGroupPostRequest request,
        CancellationToken cancellationToken = default)
    {
        var merged = request with { GroupId = id };
        var response = await _createPostHandler.HandleAsync(merged, cancellationToken);
        return Ok(response);
    }
}
