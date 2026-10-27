using Nomadica.Domain.Enums;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Domain.Entities;

public sealed class Review : BaseEntity<Guid>
{
    private Review() { }

    public UserId AuthorId { get; private set; } = default!;
    public string TargetType { get; private set; } = default!;
    public Guid TargetId { get; private set; }
    public int Rating { get; private set; }
    public string? Body { get; private set; }
    public UserId? CounterpartyId { get; private set; }
    public DateTime? AuthorSubmittedAt { get; private set; }
    public DateTime? CounterSubmittedAt { get; private set; }
    public DateTime? RevealedAt { get; private set; }
    public ReviewStatus Status { get; private set; }

    public static Review Create(UserId authorId, string targetType, Guid targetId, int rating, string? body = null, UserId? counterpartyId = null)
    {
        if (string.IsNullOrWhiteSpace(targetType))
            throw new ArgumentException("Target type cannot be empty.", nameof(targetType));
        if (rating < 1 || rating > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");

        return new Review
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            TargetType = targetType,
            TargetId = targetId,
            Rating = rating,
            Body = body,
            CounterpartyId = counterpartyId,
            Status = ReviewStatus.Pending
        };
    }

    public void TryReveal()
    {
        if (RevealedAt.HasValue || !AuthorSubmittedAt.HasValue) return;

        // Double-blind applies only when a counterparty exists; place/event reviews reveal immediately
        var counterpartyHasSubmitted = CounterpartyId is null || CounterSubmittedAt.HasValue;
        if (!counterpartyHasSubmitted) return;

        RevealedAt = DateTime.UtcNow;
        Status = ReviewStatus.Revealed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAuthorSubmittedAt()
    {
        AuthorSubmittedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCounterSubmittedAt()
    {
        CounterSubmittedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Flag()
    {
        if (Status == ReviewStatus.Removed) return;
        Status = ReviewStatus.Flagged;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Remove()
    {
        Status = ReviewStatus.Removed;
        UpdatedAt = DateTime.UtcNow;
    }
}
