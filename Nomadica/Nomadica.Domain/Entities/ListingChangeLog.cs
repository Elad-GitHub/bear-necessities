using Nomadica.Domain.Enums;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Domain.Entities;

public sealed class ListingChangeLog : BaseEntity<Guid>
{
    private ListingChangeLog() { }

    public ListingChangeTargetType TargetType { get; private set; }
    public Guid TargetId { get; private set; }
    public UserId ChangedBy { get; private set; } = default!;
    public ChangeType ChangeType { get; private set; }
    public string? OldData { get; private set; }
    public string? NewData { get; private set; }

    public static ListingChangeLog Create(
        ListingChangeTargetType targetType,
        Guid targetId,
        UserId changedBy,
        ChangeType changeType,
        string? oldData = null,
        string? newData = null)
    {
        return new ListingChangeLog
        {
            Id = Guid.NewGuid(),
            TargetType = targetType,
            TargetId = targetId,
            ChangedBy = changedBy,
            ChangeType = changeType,
            OldData = oldData,
            NewData = newData
        };
    }
}
