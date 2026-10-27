using Nomadica.Domain.Enums;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Domain.Entities;

public sealed class SavedItem
{
    private SavedItem() { }

    public UserId UserId { get; private set; } = default!;
    public string TargetType { get; private set; } = default!;
    public Guid TargetId { get; private set; }
    public SavedItemType ItemType { get; private set; }
    public DateTime SavedAt { get; private set; }

    public static SavedItem Create(
        UserId userId,
        string targetType,
        Guid targetId,
        SavedItemType itemType = SavedItemType.WantToGo)
    {
        if (string.IsNullOrWhiteSpace(targetType))
            throw new ArgumentException("Target type cannot be empty.", nameof(targetType));

        return new SavedItem
        {
            UserId = userId,
            TargetType = targetType,
            TargetId = targetId,
            ItemType = itemType,
            SavedAt = DateTime.UtcNow
        };
    }

    public void ChangeType(SavedItemType itemType)
    {
        ItemType = itemType;
        SavedAt = DateTime.UtcNow;
    }
}
