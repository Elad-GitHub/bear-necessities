using Nomadica.Application.Exceptions;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Entities;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Application.UseCases.Messages.SendMessage;

public sealed class SendMessageHandler : IRequestHandler<SendMessageRequest, SendMessageResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public SendMessageHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<SendMessageResponse> HandleAsync(
        SendMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        if (string.IsNullOrWhiteSpace(request.Body))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["body"] = ["Message body cannot be empty."]
            });

        if (request.Body.Length > 5000)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["body"] = ["Message body cannot exceed 5000 characters."]
            });

        var senderId = _currentUser.UserId;
        var recipientId = new UserId(request.RecipientId);

        if (senderId == recipientId)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["recipientId"] = ["Cannot send a message to yourself."]
            });

        var recipient = await _unitOfWork.Users.GetByIdAsync(recipientId, cancellationToken)
            ?? throw new NotFoundException("User", request.RecipientId);

        var conversation = await _unitOfWork.Conversations.GetByParticipantsAsync(
            senderId, recipientId, cancellationToken);

        if (conversation is null)
        {
            conversation = Conversation.Create();
            conversation.AddParticipant(ConversationParticipant.Create(conversation.Id, senderId));
            conversation.AddParticipant(ConversationParticipant.Create(conversation.Id, recipientId));
            await _unitOfWork.Conversations.CreateAsync(conversation, cancellationToken);
        }

        var message = Message.Create(conversation.Id, senderId, request.Body);
        conversation.UpdateLastMessageAt();

        await _unitOfWork.Conversations.AddMessageAsync(message, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SendMessageResponse(conversation.Id, message.Id);
    }
}
