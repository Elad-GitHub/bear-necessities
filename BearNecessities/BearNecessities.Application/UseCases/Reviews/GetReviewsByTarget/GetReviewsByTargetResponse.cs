using BearNecessities.Application.DTOs.Reviews;

namespace BearNecessities.Application.UseCases.Reviews.GetReviewsByTarget;

public record GetReviewsByTargetResponse(
    IReadOnlyList<ReviewDto> Items,
    Guid? NextCursor,
    decimal? AverageRating,
    int TotalCount);
