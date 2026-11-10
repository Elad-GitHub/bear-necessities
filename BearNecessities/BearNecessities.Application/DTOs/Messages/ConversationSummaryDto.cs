namespace BearNecessities.Application.DTOs.Messages;

public record ConversationSummaryDto(
    Guid Id,
    Guid OtherUserId,
    string OtherUserDisplayName,
    string? OtherUserAvatarUrl,
    string? LastMessagePreview,
    DateTime? LastMessageAt,
    int UnreadCount);
