using Nomadica.Domain.Enums;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Domain.Entities;

public sealed class GroupMembership
{
    private GroupMembership() { }

    public GroupId GroupId { get; private set; } = default!;
    public UserId UserId { get; private set; } = default!;
    public GroupRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }

    public static GroupMembership Create(GroupId groupId, UserId userId, GroupRole role = GroupRole.Member)
    {
        return new GroupMembership
        {
            GroupId = groupId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };
    }
}
