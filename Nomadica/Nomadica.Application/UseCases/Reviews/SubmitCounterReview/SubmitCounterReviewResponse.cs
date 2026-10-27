namespace Nomadica.Application.UseCases.Reviews.SubmitCounterReview;

public record SubmitCounterReviewResponse(Guid OriginalReviewId, Guid CounterReviewId, string Status);
