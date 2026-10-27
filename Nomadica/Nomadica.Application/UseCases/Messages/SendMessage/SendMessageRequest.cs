namespace Nomadica.Application.UseCases.Messages.SendMessage;

public record SendMessageRequest(Guid RecipientId, string Body);
