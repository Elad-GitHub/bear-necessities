namespace Nomadica.Application.DTOs.Messages;

public record MessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    string SenderDisplayName,
    string Body,
    bool IsDeleted,
    DateTime CreatedAt);
