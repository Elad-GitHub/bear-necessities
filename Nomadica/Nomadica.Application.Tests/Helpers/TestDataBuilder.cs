using Nomadica.Domain.Entities;
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
}
