using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BearNecessities.Application.UseCases.Messages.DeleteMessage;
using BearNecessities.Application.UseCases.Messages.GetConversations;
using BearNecessities.Application.UseCases.Messages.GetMessages;
using BearNecessities.Application.UseCases.Messages.GetUnreadCount;
using BearNecessities.Application.UseCases.Messages.SendMessage;

namespace BearNecessities.API.Controllers;

[Authorize]
public class MessagesController : BaseApiController
{
    private readonly SendMessageHandler _sendHandler;
    private readonly GetConversationsHandler _conversationsHandler;
    private readonly GetMessagesHandler _messagesHandler;
    private readonly DeleteMessageHandler _deleteHandler;
    private readonly GetUnreadCountHandler _unreadHandler;

    public MessagesController(
        SendMessageHandler sendHandler,
        GetConversationsHandler conversationsHandler,
        GetMessagesHandler messagesHandler,
        DeleteMessageHandler deleteHandler,
        GetUnreadCountHandler unreadHandler)
    {
        _sendHandler = sendHandler;
        _conversationsHandler = conversationsHandler;
        _messagesHandler = messagesHandler;
        _deleteHandler = deleteHandler;
        _unreadHandler = unreadHandler;
    }

    [HttpPost]
    public async Task<IActionResult> Send(
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _sendHandler.HandleAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetConversations(
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _conversationsHandler.HandleAsync(
            new GetConversationsRequest(pageSize, cursor), cancellationToken);
        return Ok(response);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken = default)
    {
        var response = await _unreadHandler.HandleAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("{conversationId:guid}")]
    public async Task<IActionResult> GetMessages(
        Guid conversationId,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _messagesHandler.HandleAsync(
            new GetMessagesRequest(conversationId, pageSize, cursor), cancellationToken);
        return Ok(response);
    }

    [HttpDelete("{messageId:guid}")]
    public async Task<IActionResult> Delete(Guid messageId, CancellationToken cancellationToken = default)
    {
        await _deleteHandler.HandleAsync(new DeleteMessageRequest(messageId), cancellationToken);
        return NoContent();
    }
}
