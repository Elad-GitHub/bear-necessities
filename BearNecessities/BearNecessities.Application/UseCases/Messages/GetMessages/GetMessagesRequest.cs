namespace BearNecessities.Application.UseCases.Messages.GetMessages;

public record GetMessagesRequest(Guid ConversationId, int PageSize = 20, Guid? Cursor = null);
