using Nomadica.Application.DTOs.Messages;

namespace Nomadica.Application.UseCases.Messages.GetConversations;

public record GetConversationsResponse(IReadOnlyList<ConversationSummaryDto> Items, Guid? NextCursor);
