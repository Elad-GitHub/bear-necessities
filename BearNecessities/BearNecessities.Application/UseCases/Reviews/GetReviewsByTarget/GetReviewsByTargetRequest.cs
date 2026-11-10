namespace BearNecessities.Application.UseCases.Reviews.GetReviewsByTarget;

public record GetReviewsByTargetRequest(
    string TargetType,
    Guid TargetId,
    int PageSize = 10,
    Guid? Cursor = null);
