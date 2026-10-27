using Nomadica.Domain.Entities;
using Nomadica.Domain.Enums;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Application.Tests.Helpers;

public static class TestDataBuilder
{
    public static User BuildUser(string email = "test@test.com", string displayName = "Test User")
    {
        var user = User.Create(email, displayName);
        user.SetPasswordHash("hashed_password_123");
        user.ApproveMembership();
        return user;
    }

    public static Review BuildReview(UserId authorId, string targetType, Guid targetId)
    {
        var review = Review.Create(authorId, targetType, targetId, 5, "Great place!", null);
        review.SetAuthorSubmittedAt();
        return review;
    }

    public static Place BuildPlace(UserId ownerId, ListingStatus status = ListingStatus.PendingReview)
    {
        var place = Place.Create("Test Place", 38.0m, 23.7m, "Europe/Athens", ownerId);
        MovePlaceToStatus(place, status);
        return place;
    }

    public static Event BuildEvent(UserId ownerId, ListingStatus status = ListingStatus.PendingReview)
    {
        var evt = Event.Create("Test Event", EventType.OneTime, "Europe/Athens", ownerId);
        if (status == ListingStatus.Draft)
            return evt;

        evt.Submit();
        if (status == ListingStatus.Live)
            evt.Approve(new UserId(Guid.NewGuid()));
        else if (status == ListingStatus.Cancelled)
            evt.Cancel();
        return evt;
    }

    private static void MovePlaceToStatus(Place place, ListingStatus status)
    {
        if (status == ListingStatus.Draft)
            return;

        place.Submit();
        if (status == ListingStatus.Live)
            place.Approve(new UserId(Guid.NewGuid()));
        else if (status == ListingStatus.Suspended)
            place.Reject(new UserId(Guid.NewGuid()));
        else if (status == ListingStatus.Archived)
            place.Archive();
    }
}
