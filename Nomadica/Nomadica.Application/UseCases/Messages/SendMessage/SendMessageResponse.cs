namespace Nomadica.Application.UseCases.Messages.SendMessage;

public record SendMessageResponse(Guid ConversationId, Guid MessageId);
