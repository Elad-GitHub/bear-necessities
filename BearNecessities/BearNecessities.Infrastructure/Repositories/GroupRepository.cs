using Microsoft.EntityFrameworkCore;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;
using BearNecessities.Infrastructure.Persistence;

namespace BearNecessities.Infrastructure.Repositories;

public sealed class GroupRepository : IGroupRepository
{
    private readonly BearNecessitiesDbContext _context;

    public GroupRepository(BearNecessitiesDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(CommunityGroup group, CancellationToken cancellationToken = default)
        => await _context.CommunityGroups.AddAsync(group, cancellationToken);

    public async Task<CommunityGroup?> GetByIdAsync(GroupId id, CancellationToken cancellationToken = default)
        => await _context.CommunityGroups.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public async Task<List<CommunityGroup>> SearchAsync(
        string? query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var groups = _context.CommunityGroups.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = $"%{query.Trim()}%";
            groups = groups.Where(g =>
                EF.Functions.Like(g.Name, pattern)
                || EF.Functions.Like(g.City!, pattern)
                || EF.Functions.Like(g.Country!, pattern));
        }

        return await groups
            .OrderByDescending(g => g.MemberCount)
            .ThenBy(g => g.Name)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public Task UpdateAsync(CommunityGroup group, CancellationToken cancellationToken = default)
    {
        _context.CommunityGroups.Update(group);
        return Task.CompletedTask;
    }

    public async Task AddMembershipAsync(GroupMembership membership, CancellationToken cancellationToken = default)
        => await _context.GroupMemberships.AddAsync(membership, cancellationToken);

    public async Task<GroupMembership?> GetMembershipAsync(
        GroupId groupId,
        UserId userId,
        CancellationToken cancellationToken = default)
        => await _context.GroupMemberships
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId, cancellationToken);

    public void RemoveMembership(GroupMembership membership)
        => _context.GroupMemberships.Remove(membership);

    public async Task<List<GroupId>> GetMyGroupIdsAsync(UserId userId, CancellationToken cancellationToken = default)
        => await _context.GroupMemberships
            .Where(m => m.UserId == userId)
            .Select(m => m.GroupId)
            .ToListAsync(cancellationToken);

    public async Task<List<CommunityGroup>> GetMyGroupsAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var groupIds = await GetMyGroupIdsAsync(userId, cancellationToken);
        return await _context.CommunityGroups
            .Where(g => groupIds.Contains(g.Id))
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddPostAsync(GroupPost post, CancellationToken cancellationToken = default)
        => await _context.GroupPosts.AddAsync(post, cancellationToken);

    public async Task<GroupPost?> GetPostByIdAsync(Guid postId, CancellationToken cancellationToken = default)
        => await _context.GroupPosts.FirstOrDefaultAsync(p => p.Id == postId, cancellationToken);

    public async Task<List<GroupPost>> GetPostsByGroupAsync(
        GroupId groupId,
        int pageSize,
        Guid? cursor,
        CancellationToken cancellationToken = default)
    {
        var posts = await _context.GroupPosts
            .Where(p => p.GroupId == groupId && !p.IsRemoved)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        var startIndex = 0;
        if (cursor.HasValue)
        {
            var index = posts.FindIndex(p => p.Id == cursor.Value);
            if (index >= 0) startIndex = index + 1;
        }

        return posts.Skip(startIndex).Take(pageSize).ToList();
    }
}
