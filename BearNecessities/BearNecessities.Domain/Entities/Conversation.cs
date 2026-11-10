namespace BearNecessities.Domain.Entities;

public sealed class Conversation : BaseEntity<Guid>
{
    private readonly List<ConversationParticipant> _participants = new();
    private readonly List<Message> _messages = new();

    private Conversation() { }

    public string? ContextType { get; private set; }
    public Guid? ContextId { get; private set; }
    public DateTime? LastMessageAt { get; private set; }

    public IReadOnlyCollection<ConversationParticipant> Participants => _participants.AsReadOnly();
    public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

    public static Conversation Create(string? contextType = null, Guid? contextId = null)
    {
        return new Conversation
        {
            Id = Guid.NewGuid(),
            ContextType = contextType,
            ContextId = contextId
        };
    }

    public void AddParticipant(ConversationParticipant participant)
    {
        _participants.Add(participant);
    }

    public void UpdateLastMessageAt()
    {
        LastMessageAt = DateTime.UtcNow;
    }
}
