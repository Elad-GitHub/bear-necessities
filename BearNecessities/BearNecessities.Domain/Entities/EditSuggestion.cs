using BearNecessities.Domain.Enums;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Domain.Entities;

/// <summary>
/// An edit someone proposed for a place or event. The listing keeps showing its current details
/// until an admin approves; <see cref="ProposedChanges"/> holds the edit request as JSON.
/// </summary>
public sealed class EditSuggestion : BaseEntity<Guid>
{
    private EditSuggestion() { }

    public ListingChangeTargetType TargetType { get; private set; }
    public Guid TargetId { get; private set; }
    public UserId SuggestedBy { get; private set; } = default!;
    public string ProposedChanges { get; private set; } = default!;
    public EditSuggestionStatus Status { get; private set; }
    public UserId? ReviewedBy { get; private set; }
    public DateTime? ReviewedAt { get; private set; }

    public static EditSuggestion Create(ListingChangeTargetType targetType, Guid targetId, UserId suggestedBy, string proposedChanges)
    {
        if (string.IsNullOrWhiteSpace(proposedChanges))
            throw new ArgumentException("Proposed changes cannot be empty.", nameof(proposedChanges));

        return new EditSuggestion
        {
            Id = Guid.NewGuid(),
            TargetType = targetType,
            TargetId = targetId,
            SuggestedBy = suggestedBy,
            ProposedChanges = proposedChanges,
            Status = EditSuggestionStatus.Pending
        };
    }

    public void Approve(UserId reviewerId) => Review(reviewerId, EditSuggestionStatus.Approved);

    public void Reject(UserId reviewerId) => Review(reviewerId, EditSuggestionStatus.Rejected);

    private void Review(UserId reviewerId, EditSuggestionStatus outcome)
    {
        if (Status != EditSuggestionStatus.Pending)
            throw new InvalidOperationException("Only pending suggestions can be reviewed.");

        Status = outcome;
        ReviewedBy = reviewerId;
        ReviewedAt = DateTime.UtcNow;
    }
}
