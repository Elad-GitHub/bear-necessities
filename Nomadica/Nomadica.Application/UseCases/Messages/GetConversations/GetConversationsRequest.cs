namespace Nomadica.Application.UseCases.Messages.GetConversations;

public record GetConversationsRequest(int PageSize = 20, Guid? Cursor = null);
