using Nomadica.Domain.Entities;
using Nomadica.Domain.Enums;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Domain.Tests.Entities;

public class ReviewTests
{
    [Fact]
    public void TryReveal_PlaceReviewWithoutCounterparty_ShouldRevealImmediately()
    {
        // Arrange — no counterparty means double-blind does not apply
        var review = Review.Create(
            new UserId(Guid.NewGuid()),
            "Place",
            Guid.NewGuid(),
            5,
            "Great place!",
            null);
        review.SetAuthorSubmittedAt();

        // Act
        review.TryReveal();

        // Assert
        review.RevealedAt.Should().NotBeNull();
        review.Status.Should().Be(ReviewStatus.Revealed);
    }

    [Fact]
    public void TryReveal_WithCounterpartyOnlyAuthorSubmitted_ShouldNotReveal()
    {
        // Arrange — counterparty exists, so double-blind holds until both submit
        var review = Review.Create(
            new UserId(Guid.NewGuid()),
            "User",
            Guid.NewGuid(),
            5,
            "Great traveler!",
            new UserId(Guid.NewGuid()));
        review.SetAuthorSubmittedAt();

        // Act
        review.TryReveal();

        // Assert
        review.RevealedAt.Should().BeNull();
        review.Status.Should().Be(ReviewStatus.Pending);
    }

    [Fact]
    public void TryReveal_WhenBothSubmitted_ShouldSetRevealedAt()
    {
        // Arrange
        var review = Review.Create(
            new UserId(Guid.NewGuid()),
            "Place",
            Guid.NewGuid(),
            5,
            "Great place!",
            null);
        review.SetAuthorSubmittedAt();
        review.SetCounterSubmittedAt();
        var beforeReveal = DateTime.UtcNow;

        // Act
        review.TryReveal();

        // Assert
        review.RevealedAt.Should().NotBeNull();
        review.RevealedAt.Should().BeOnOrAfter(beforeReveal);
    }

    [Fact]
    public void TryReveal_WhenBothSubmitted_ShouldSetStatusToRevealed()
    {
        // Arrange
        var review = Review.Create(
            new UserId(Guid.NewGuid()),
            "Place",
            Guid.NewGuid(),
            5,
            "Great place!",
            null);
        review.SetAuthorSubmittedAt();
        review.SetCounterSubmittedAt();

        // Act
        review.TryReveal();

        // Assert
        review.Status.Should().Be(ReviewStatus.Revealed);
    }

    [Fact]
    public void Flag_ShouldSetStatusToFlagged()
    {
        // Arrange
        var review = Review.Create(
            new UserId(Guid.NewGuid()),
            "Place",
            Guid.NewGuid(),
            5,
            "Great place!",
            null);

        // Act
        review.Flag();

        // Assert
        review.Status.Should().Be(ReviewStatus.Flagged);
    }

    [Fact]
    public void Remove_ShouldSetStatusToRemoved()
    {
        // Arrange
        var review = Review.Create(
            new UserId(Guid.NewGuid()),
            "Place",
            Guid.NewGuid(),
            5,
            "Great place!",
            null);

        // Act
        review.Remove();

        // Assert
        review.Status.Should().Be(ReviewStatus.Removed);
    }
}
