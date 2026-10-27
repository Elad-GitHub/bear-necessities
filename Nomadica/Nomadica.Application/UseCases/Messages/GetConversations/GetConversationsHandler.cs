using Nomadica.Application.DTOs.Messages;
using Nomadica.Application.Exceptions;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Interfaces;

namespace Nomadica.Application.UseCases.Messages.GetConversations;

public sealed class GetConversationsHandler : IRequestHandler<GetConversationsRequest, GetConversationsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetConversationsHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<GetConversationsResponse> HandleAsync(
        GetConversationsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        var userId = _currentUser.UserId;

        var conversations = await _unitOfWork.Conversations.GetUserConversationsAsync(
            userId, request.PageSize, request.Cursor, cancellationToken);

        if (conversations.Count == 0)
            return new GetConversationsResponse([], null);

        var conversationIds = conversations.Select(c => c.Id).ToList();

        var otherUserIds = conversations
            .SelectMany(c => c.Participants.Where(p => p.UserId != userId).Select(p => p.UserId))
            .Distinct()
            .ToList();

        var usersTask = _unitOfWork.Users.GetByIdsAsync(otherUserIds, cancellationToken);
        var previewsTask = _unitOfWork.Conversations.GetLastMessagePreviewsAsync(conversationIds, cancellationToken);
        var unreadTask = _unitOfWork.Conversations.GetUnreadCountsPerConversationAsync(userId, conversationIds, cancellationToken);

        await Task.WhenAll(usersTask, previewsTask, unreadTask);

        var userMap = usersTask.Result.ToDictionary(u => u.Id);
        var previews = previewsTask.Result;
        var unreadCounts = unreadTask.Result;

        var items = conversations.Select(c =>
        {
            var otherParticipant = c.Participants.FirstOrDefault(p => p.UserId != userId);
            userMap.TryGetValue(otherParticipant?.UserId ?? userId, out var otherUser);

            previews.TryGetValue(c.Id, out var preview);
            unreadCounts.TryGetValue(c.Id, out var unread);

            return new ConversationSummaryDto(
                c.Id,
                otherUser?.Id.Value ?? Guid.Empty,
                otherUser?.DisplayName ?? string.Empty,
                otherUser?.AvatarUrl,
                preview,
                c.LastMessageAt,
                unread);
        }).ToList();

        Guid? nextCursor = items.Count == request.PageSize ? items[^1].Id : null;
        return new GetConversationsResponse(items, nextCursor);
    }
}
