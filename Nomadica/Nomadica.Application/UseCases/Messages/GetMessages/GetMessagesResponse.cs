using Nomadica.Application.DTOs.Messages;

namespace Nomadica.Application.UseCases.Messages.GetMessages;

public record GetMessagesResponse(IReadOnlyList<MessageDto> Items, Guid? NextCursor);
