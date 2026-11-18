using BearNecessities.Domain.Enums;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Domain.Entities;

public sealed class Media : BaseEntity<Guid>
{
    private Media() { }

    public MediaOwnerType OwnerType { get; private set; }
    public Guid OwnerId { get; private set; }
    public string StoragePath { get; private set; } = default!;
    public string Url { get; private set; } = default!;
    public string? ThumbnailUrl { get; private set; }
    public string MimeType { get; private set; } = default!;
    public long SizeBytes { get; private set; }
    public ModerationStatus ModerationStatus { get; private set; }
    public int SortOrder { get; private set; }
    public UserId UploadedBy { get; private set; } = default!;
    /// <summary>Set when the photo was shared with a review; it then also shows in the place's gallery.</summary>
    public Guid? ReviewId { get; private set; }

    public static Media Create(
        MediaOwnerType ownerType,
        Guid ownerId,
        string storagePath,
        string url,
        string mimeType,
        long sizeBytes,
        UserId uploadedBy,
        string? thumbnailUrl = null,
        int sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
            throw new ArgumentException("Storage path cannot be empty.", nameof(storagePath));
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty.", nameof(url));
        if (string.IsNullOrWhiteSpace(mimeType))
            throw new ArgumentException("MIME type cannot be empty.", nameof(mimeType));
        if (sizeBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Size must be greater than zero.");

        return new Media
        {
            Id = Guid.NewGuid(),
            OwnerType = ownerType,
            OwnerId = ownerId,
            StoragePath = storagePath,
            Url = url,
            ThumbnailUrl = thumbnailUrl,
            MimeType = mimeType,
            SizeBytes = sizeBytes,
            ModerationStatus = ModerationStatus.Pending,
            SortOrder = sortOrder,
            UploadedBy = uploadedBy
        };
    }

    public void SetModerationStatus(ModerationStatus status)
    {
        ModerationStatus = status;
    }

    public void AttachToReview(Guid reviewId) => ReviewId = reviewId;

    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }
}
