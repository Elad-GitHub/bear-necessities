namespace Nomadica.Application.UseCases.Reviews.SubmitCounterReview;

public record SubmitCounterReviewRequest(
    Guid OriginalReviewId,
    int Rating,
    string? Body);
