using Microsoft.EntityFrameworkCore;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;
using BearNecessities.Infrastructure.Persistence;

namespace BearNecessities.Infrastructure.Repositories;

public sealed class ConversationRepository : IConversationRepository
{
    private readonly BearNecessitiesDbContext _context;

    public ConversationRepository(BearNecessitiesDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(Conversation conversation, CancellationToken cancellationToken = default)
        => await _context.Conversations.AddAsync(conversation, cancellationToken);

    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<Conversation?> GetByParticipantsAsync(
        UserId user1,
        UserId user2,
        CancellationToken cancellationToken = default)
        => await _context.Conversations
            .Include(c => c.Participants)
            .Where(c =>
                _context.ConversationParticipants.Any(p => p.ConversationId == c.Id && p.UserId == user1) &&
                _context.ConversationParticipants.Any(p => p.ConversationId == c.Id && p.UserId == user2))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<List<Conversation>> GetUserConversationsAsync(
        UserId userId,
        int pageSize,
        Guid? cursor,
        CancellationToken cancellationToken = default)
    {
        var all = await _context.Conversations
            .Include(c => c.Participants)
            .Where(c => _context.ConversationParticipants.Any(p => p.ConversationId == c.Id && p.UserId == userId))
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ToListAsync(cancellationToken);

        var startIndex = 0;
        if (cursor.HasValue)
        {
            var idx = all.FindIndex(c => c.Id == cursor.Value);
            startIndex = idx >= 0 ? idx + 1 : 0;
        }

        return all.Skip(startIndex).Take(pageSize).ToList();
    }

    public async Task AddMessageAsync(Message message, CancellationToken cancellationToken = default)
        => await _context.Messages.AddAsync(message, cancellationToken);

    public async Task<Message?> GetMessageByIdAsync(Guid messageId, CancellationToken cancellationToken = default)
        => await _context.Messages.FindAsync(new object?[] { messageId }, cancellationToken);

    public async Task<List<Message>> GetMessagesAsync(
        Guid conversationId,
        int pageSize,
        Guid? cursor,
        CancellationToken cancellationToken = default)
    {
        var all = await _context.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id)
            .ToListAsync(cancellationToken);

        var startIndex = 0;
        if (cursor.HasValue)
        {
            var idx = all.FindIndex(m => m.Id == cursor.Value);
            startIndex = idx >= 0 ? idx + 1 : 0;
        }

        return all.Skip(startIndex).Take(pageSize).ToList();
    }

    public async Task MarkAsReadAsync(
        Guid conversationId,
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        var participant = await _context.ConversationParticipants
            .FirstOrDefaultAsync(
                p => p.ConversationId == conversationId && p.UserId == userId,
                cancellationToken);
        participant?.MarkAsRead();
    }

    public async Task<int> GetUnreadCountAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await (
            from msg in _context.Messages
            join cp in _context.ConversationParticipants on msg.ConversationId equals cp.ConversationId
            where cp.UserId == userId
                  && msg.SenderId != userId
                  && !msg.IsDeleted
                  && (cp.LastReadAt == null || msg.CreatedAt > cp.LastReadAt)
            select msg
        ).CountAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, string>> GetLastMessagePreviewsAsync(
        IEnumerable<Guid> conversationIds,
        CancellationToken cancellationToken = default)
    {
        var ids = conversationIds.ToList();
        var messages = await _context.Messages
            .Where(m => ids.Contains(m.ConversationId) && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        return messages
            .GroupBy(m => m.ConversationId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var body = g.First().Body;
                    return body.Length <= 100 ? body : body[..100];
                });
    }

    public async Task<Dictionary<Guid, int>> GetUnreadCountsPerConversationAsync(
        UserId userId,
        IEnumerable<Guid> conversationIds,
        CancellationToken cancellationToken = default)
    {
        var ids = conversationIds.ToList();
        var results = await (
            from msg in _context.Messages
            join cp in _context.ConversationParticipants on msg.ConversationId equals cp.ConversationId
            where ids.Contains(msg.ConversationId)
                  && cp.UserId == userId
                  && msg.SenderId != userId
                  && !msg.IsDeleted
                  && (cp.LastReadAt == null || msg.CreatedAt > cp.LastReadAt)
            group msg by msg.ConversationId into g
            select new { ConversationId = g.Key, Count = g.Count() }
        ).ToListAsync(cancellationToken);

        return results.ToDictionary(x => x.ConversationId, x => x.Count);
    }
}
