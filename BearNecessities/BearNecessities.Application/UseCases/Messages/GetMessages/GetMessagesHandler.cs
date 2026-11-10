using BearNecessities.Application.DTOs.Messages;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Interfaces;

namespace BearNecessities.Application.UseCases.Messages.GetMessages;

public sealed class GetMessagesHandler : IRequestHandler<GetMessagesRequest, GetMessagesResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetMessagesHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<GetMessagesResponse> HandleAsync(
        GetMessagesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        var userId = _currentUser.UserId;

        var conversation = await _unitOfWork.Conversations.GetByIdAsync(request.ConversationId, cancellationToken)
            ?? throw new NotFoundException("Conversation", request.ConversationId);

        if (!conversation.Participants.Any(p => p.UserId == userId))
            throw new ForbiddenException("You are not a participant in this conversation.");

        var messages = await _unitOfWork.Conversations.GetMessagesAsync(
            request.ConversationId, request.PageSize, request.Cursor, cancellationToken);

        var senderIds = messages.Select(m => m.SenderId).Distinct().ToList();
        var senders = await _unitOfWork.Users.GetByIdsAsync(senderIds, cancellationToken);
        var senderMap = senders.ToDictionary(u => u.Id);

        await _unitOfWork.Conversations.MarkAsReadAsync(request.ConversationId, userId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var items = messages.Select(m =>
        {
            senderMap.TryGetValue(m.SenderId, out var sender);
            return new MessageDto(
                m.Id,
                m.ConversationId,
                m.SenderId.Value,
                sender?.DisplayName ?? string.Empty,
                m.Body,
                m.IsDeleted,
                m.CreatedAt);
        }).ToList();

        Guid? nextCursor = items.Count == request.PageSize ? items[^1].Id : null;
        return new GetMessagesResponse(items, nextCursor);
    }
}
