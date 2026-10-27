using Nomadica.Domain.Entities;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Domain.Interfaces;

public interface IConversationRepository
{
    Task CreateAsync(Conversation conversation, CancellationToken cancellationToken = default);
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Conversation?> GetByParticipantsAsync(UserId user1, UserId user2, CancellationToken cancellationToken = default);
    Task<List<Conversation>> GetUserConversationsAsync(UserId userId, int pageSize, Guid? cursor, CancellationToken cancellationToken = default);
    Task AddMessageAsync(Message message, CancellationToken cancellationToken = default);
    Task<Message?> GetMessageByIdAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task<List<Message>> GetMessagesAsync(Guid conversationId, int pageSize, Guid? cursor, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(Guid conversationId, UserId userId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, string>> GetLastMessagePreviewsAsync(IEnumerable<Guid> conversationIds, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, int>> GetUnreadCountsPerConversationAsync(UserId userId, IEnumerable<Guid> conversationIds, CancellationToken cancellationToken = default);
}
