namespace Nomadica.Application.UseCases.Reviews.AdminRemoveReview;

public record AdminRemoveReviewRequest(Guid ReviewId, string Reason);
