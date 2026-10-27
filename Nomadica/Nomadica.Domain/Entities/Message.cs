using Nomadica.Domain.ValueObjects;

namespace Nomadica.Domain.Entities;

public sealed class Message : BaseEntity<Guid>
{
    private Message() { }

    public Guid ConversationId { get; private set; }
    public UserId SenderId { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public bool IsDeleted { get; private set; }

    public static Message Create(Guid conversationId, UserId senderId, string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Message body cannot be empty.", nameof(body));

        return new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = senderId,
            Body = body,
            IsDeleted = false
        };
    }

    public void MarkAsDeleted()
    {
        IsDeleted = true;
    }
}
