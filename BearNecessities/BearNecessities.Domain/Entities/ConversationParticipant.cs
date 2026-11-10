using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Domain.Entities;

public sealed class ConversationParticipant
{
    private ConversationParticipant() { }

    public Guid ConversationId { get; private set; }
    public UserId UserId { get; private set; } = default!;
    public DateTime JoinedAt { get; private set; }
    public DateTime? LastReadAt { get; private set; }

    public static ConversationParticipant Create(Guid conversationId, UserId userId)
    {
        return new ConversationParticipant
        {
            ConversationId = conversationId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        };
    }

    public void MarkAsRead()
    {
        LastReadAt = DateTime.UtcNow;
    }
}
