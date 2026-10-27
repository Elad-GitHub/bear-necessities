using Nomadica.Application.DTOs.Reviews;

namespace Nomadica.Application.UseCases.Reviews.GetReviewsByTarget;

public record GetReviewsByTargetResponse(
    IReadOnlyList<ReviewDto> Items,
    Guid? NextCursor,
    decimal? AverageRating,
    int TotalCount);
