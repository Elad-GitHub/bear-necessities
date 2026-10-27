namespace Nomadica.Application.UseCases.Reviews.CreateReview;

public record CreateReviewRequest(
    string TargetType,
    Guid TargetId,
    int Rating,
    string? Body,
    Guid? CounterpartyId);
