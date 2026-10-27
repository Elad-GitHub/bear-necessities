namespace Nomadica.Application.UseCases.Reviews.FlagReview;

public record FlagReviewRequest(Guid ReviewId, string Reason);
