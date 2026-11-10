using BearNecessities.Application.DTOs.Messages;

namespace BearNecessities.Application.UseCases.Messages.GetConversations;

public record GetConversationsResponse(IReadOnlyList<ConversationSummaryDto> Items, Guid? NextCursor);
