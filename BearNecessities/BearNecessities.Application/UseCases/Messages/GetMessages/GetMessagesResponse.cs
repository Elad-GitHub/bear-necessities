using BearNecessities.Application.DTOs.Messages;

namespace BearNecessities.Application.UseCases.Messages.GetMessages;

public record GetMessagesResponse(IReadOnlyList<MessageDto> Items, Guid? NextCursor);
