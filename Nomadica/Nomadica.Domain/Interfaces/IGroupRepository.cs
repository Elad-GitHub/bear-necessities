using Nomadica.Domain.Entities;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Domain.Interfaces;

public interface IGroupRepository
{
    Task AddAsync(CommunityGroup group, CancellationToken cancellationToken = default);
    Task<CommunityGroup?> GetByIdAsync(GroupId id, CancellationToken cancellationToken = default);
    Task<List<CommunityGroup>> SearchAsync(string? query, int limit, CancellationToken cancellationToken = default);
    Task UpdateAsync(CommunityGroup group, CancellationToken cancellationToken = default);

    Task AddMembershipAsync(GroupMembership membership, CancellationToken cancellationToken = default);
    Task<GroupMembership?> GetMembershipAsync(GroupId groupId, UserId userId, CancellationToken cancellationToken = default);
    void RemoveMembership(GroupMembership membership);
    Task<List<GroupId>> GetMyGroupIdsAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<List<CommunityGroup>> GetMyGroupsAsync(UserId userId, CancellationToken cancellationToken = default);

    Task AddPostAsync(GroupPost post, CancellationToken cancellationToken = default);
    Task<GroupPost?> GetPostByIdAsync(Guid postId, CancellationToken cancellationToken = default);
    Task<List<GroupPost>> GetPostsByGroupAsync(GroupId groupId, int pageSize, Guid? cursor, CancellationToken cancellationToken = default);
}
